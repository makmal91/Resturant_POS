import React, { useEffect, useMemo, useState } from 'react';
import { masterDataService, type MasterType } from '../../services/masterDataService';

export interface MasterSelectProps {
  source: MasterType;
  value: string | number;
  onChange: (value: string) => void;
  branchId?: number;
  countryId?: number;
  placeholder?: string;
  disabled?: boolean;
  className?: string;
  /** When true, stores the master name instead of id (for legacy string fields). */
  valueByName?: boolean;
  /** Include current value if it is not in the master list (backward compatibility). */
  preserveCustomValue?: boolean;
}

const selectClassName =
  'w-full rounded border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500 disabled:bg-gray-50 disabled:text-gray-500';

const MasterSelect: React.FC<MasterSelectProps> = ({
  source,
  value,
  onChange,
  branchId,
  countryId,
  placeholder = 'Select…',
  disabled = false,
  className,
  valueByName = false,
  preserveCustomValue = true,
}) => {
  const [options, setOptions] = useState<{ id: number; name: string }[]>([]);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    let cancelled = false;

    const load = async () => {
      setLoading(true);
      try {
        const rows = await masterDataService.getMasterData(source, { branchId, countryId });
        if (!cancelled) {
          setOptions(rows.map((r) => ({ id: r.id, name: r.name })));
        }
      } catch {
        if (!cancelled) {
          setOptions(masterDataService.getFallbackMasterData(source).map((name, index) => ({
            id: index + 1,
            name,
          })));
        }
      } finally {
        if (!cancelled) setLoading(false);
      }
    };

    void load();
    return () => { cancelled = true; };
  }, [source, branchId, countryId]);

  const selectOptions = useMemo(() => {
    const base = options.map((opt) => ({
      label: opt.name,
      value: valueByName ? opt.name : String(opt.id),
    }));

    const current = String(value ?? '').trim();
    if (
      preserveCustomValue &&
      current &&
      !base.some((opt) => opt.value === current)
    ) {
      return [{ label: current, value: current }, ...base];
    }

    return base;
  }, [options, value, valueByName, preserveCustomValue]);

  return (
    <select
      value={String(value ?? '')}
      onChange={(event) => onChange(event.target.value)}
      disabled={disabled || loading}
      className={className ?? selectClassName}
    >
      <option value="">{loading ? 'Loading…' : placeholder}</option>
      {selectOptions.map((opt) => (
        <option key={`${opt.value}-${opt.label}`} value={opt.value}>
          {opt.label}
        </option>
      ))}
    </select>
  );
};

export default MasterSelect;

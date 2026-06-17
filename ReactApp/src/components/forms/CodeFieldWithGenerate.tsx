import React, { useEffect, useMemo, useRef, useState } from 'react';
import { CodeModuleName, codeGeneratorService } from '../../services/codeGeneratorService';
import { getApiErrorMessage } from '../../services/api';
import { useBranchStore } from '../../stores/useBranchStore';
import { resolveEffectiveBranchId } from '../../utils/resolveBranchId';

interface CodeFieldWithGenerateProps {
  label: string;
  name: string;
  value: string;
  onChange: (value: string) => void;
  module: CodeModuleName;
  branchId?: number;
  placeholder?: string;
  error?: string;
  disabled?: boolean;
  required?: boolean;
  /** When true, shows the existing code read-only without fetching a new one. */
  isEditMode?: boolean;
  /** Increment to request a fresh auto-generated code (create mode only). */
  resetKey?: number | string;
  /** Match compact inputs (e.g. ProductForm) instead of standard FormInput spacing. */
  variant?: 'default' | 'compact';
}

const CodeFieldWithGenerate: React.FC<CodeFieldWithGenerateProps> = ({
  label,
  name,
  value,
  onChange,
  module,
  branchId,
  placeholder = 'Generating…',
  error,
  disabled = false,
  required = false,
  isEditMode = false,
  resetKey,
  variant = 'default',
}) => {
  const selectedBranchId = useBranchStore((state) => state.selectedBranchId);
  const effectiveBranchId = useMemo(
    () => (module === 'Branch' ? undefined : resolveEffectiveBranchId(branchId, selectedBranchId)),
    [branchId, module, selectedBranchId],
  );

  const [isLoading, setIsLoading] = useState(false);
  const [loadError, setLoadError] = useState('');
  const requestIdRef = useRef(0);
  const onChangeRef = useRef(onChange);
  onChangeRef.current = onChange;

  const branchRequired = module !== 'Branch';
  const branchReady = !branchRequired || Boolean(effectiveBranchId && effectiveBranchId > 0);
  const shouldAutoGenerate = !isEditMode && !disabled;

  useEffect(() => {
    if (!shouldAutoGenerate) {
      setLoadError('');
      setIsLoading(false);
      return;
    }

    if (!branchReady) {
      setLoadError('');
      setIsLoading(false);
      if (value) {
        onChangeRef.current('');
      }
      return;
    }

    const requestId = ++requestIdRef.current;
    let cancelled = false;

    const loadCode = async () => {
      setIsLoading(true);
      setLoadError('');
      try {
        const code = await codeGeneratorService.preview(module, effectiveBranchId);
        if (cancelled || requestId !== requestIdRef.current) {
          return;
        }
        onChangeRef.current(code);
      } catch (err) {
        if (cancelled || requestId !== requestIdRef.current) {
          return;
        }
        setLoadError(getApiErrorMessage(err, 'Unable to generate code.'));
      } finally {
        if (!cancelled && requestId === requestIdRef.current) {
          setIsLoading(false);
        }
      }
    };

    void loadCode();

    return () => {
      cancelled = true;
    };
  }, [module, effectiveBranchId, shouldAutoGenerate, branchReady, isEditMode, disabled, resetKey]);

  const displayPlaceholder = isEditMode
    ? ''
    : !branchReady
      ? 'Select a branch first'
      : isLoading
        ? 'Generating…'
        : placeholder;

  const fieldError = error || loadError;
  const isCompact = variant === 'compact';

  const wrapperClass = isCompact ? '' : 'mb-5';
  const labelClass = isCompact
    ? 'mb-1 block text-sm font-medium text-gray-700'
    : 'block text-sm font-medium text-gray-800 mb-2';
  const inputClass = isCompact
    ? `w-full cursor-default rounded-lg border bg-white px-3 py-2 text-gray-900 placeholder-gray-400 focus:border-blue-500 focus:outline-none ${
        fieldError ? 'border-red-300' : 'border-gray-300'
      }`
    : `w-full cursor-default rounded-lg border bg-white px-4 py-3 text-gray-900 shadow-sm placeholder-gray-400 transition-colors duration-200 focus:border-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-500 ${
        fieldError ? 'border-red-300 focus:ring-red-500 focus:border-red-500' : 'border-gray-300'
      }`;

  return (
    <div className={wrapperClass}>
      <label htmlFor={name} className={labelClass}>
        {label}
        {required && <span className="ml-1 text-red-500">*</span>}
      </label>
      <input
        id={name}
        name={name}
        type="text"
        value={value}
        readOnly
        placeholder={displayPlaceholder}
        aria-readonly="true"
        className={inputClass}
      />
      {fieldError && (
        <p className={`text-sm text-red-600 ${isCompact ? 'mt-1' : 'mt-1 flex items-center'}`}>
          {!isCompact && (
            <svg className="mr-1 h-4 w-4" fill="currentColor" viewBox="0 0 20 20" aria-hidden="true">
              <path
                fillRule="evenodd"
                d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7 4a1 1 0 11-2 0 1 1 0 012 0zm-1-9a1 1 0 00-1 1v4a1 1 0 102 0V6a1 1 0 00-1-1z"
                clipRule="evenodd"
              />
            </svg>
          )}
          {fieldError}
        </p>
      )}
    </div>
  );
};

export default CodeFieldWithGenerate;

import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';

export interface SearchableSelectOption {
  label: string;
  value: string | number;
  /** Optional secondary text (e.g. base unit) */
  hint?: string;
}

interface SearchableSelectProps {
  label: string;
  name: string;
  value: string | number;
  onChange: (name: string, value: string | number) => void;
  options: SearchableSelectOption[];
  placeholder?: string;
  required?: boolean;
  error?: string;
  disabled?: boolean;
  loading?: boolean;
  /** Compact layout for table rows — no bottom margin, smaller control */
  variant?: 'default' | 'compact';
  /** Debounce filter typing (ms). Default 200 */
  debounceMs?: number;
  /** Notifies parent of search term (debounced) for server-side filtering */
  onSearchTermChange?: (term: string) => void;
  /** Increment to open dropdown and focus search input */
  focusRequest?: number;
  /** Hide visible label (keep for a11y) */
  hideLabel?: boolean;
}

const SearchableSelect: React.FC<SearchableSelectProps> = ({
  label,
  name,
  value,
  onChange,
  options,
  placeholder = 'Select…',
  required = false,
  error,
  disabled = false,
  loading = false,
  variant = 'default',
  debounceMs = 200,
  onSearchTermChange,
  focusRequest = 0,
  hideLabel = false,
}) => {
  const [open, setOpen] = useState(false);
  const [search, setSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const containerRef = useRef<HTMLDivElement>(null);
  const searchInputRef = useRef<HTMLInputElement>(null);

  const selected = useMemo(
    () => options.find((o) => String(o.value) === String(value)),
    [options, value],
  );

  useEffect(() => {
    const timer = window.setTimeout(() => setDebouncedSearch(search), debounceMs);
    return () => window.clearTimeout(timer);
  }, [search, debounceMs]);

  useEffect(() => {
    if (!onSearchTermChange) return;
    onSearchTermChange(debouncedSearch);
  }, [debouncedSearch, onSearchTermChange]);

  const filtered = useMemo(() => {
    if (onSearchTermChange) return options;
    const term = debouncedSearch.trim().toLowerCase();
    if (!term) return options.slice(0, 150);
    return options
      .filter((o) => o.label.toLowerCase().includes(term) || o.hint?.toLowerCase().includes(term))
      .slice(0, 150);
  }, [options, debouncedSearch, onSearchTermChange]);

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setOpen(false);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  useEffect(() => {
    if (focusRequest > 0) {
      setOpen(true);
      window.setTimeout(() => searchInputRef.current?.focus(), 0);
    }
  }, [focusRequest]);

  const handleSelect = useCallback(
    (optionValue: string | number) => {
      onChange(name, optionValue);
      setOpen(false);
      setSearch('');
    },
    [name, onChange],
  );

  const handleKeyDown = (event: React.KeyboardEvent) => {
    if (event.key === 'Escape') {
      setOpen(false);
      return;
    }
    if (event.key === 'Enter' && open && filtered.length === 1) {
      event.preventDefault();
      handleSelect(filtered[0].value);
    }
  };

  const isCompact = variant === 'compact';
  const wrapperClass = isCompact ? 'mb-0' : 'mb-5';
  const buttonClass = isCompact
    ? 'w-full px-3 py-2.5 border rounded-lg text-sm text-left focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500 transition-shadow'
    : 'w-full px-4 py-3 border rounded-lg shadow-sm text-left focus:outline-none focus:ring-2 focus:ring-primary focus:border-primary transition-colors duration-200';

  return (
    <div className={wrapperClass} ref={containerRef} onKeyDown={handleKeyDown}>
      <label
        htmlFor={name}
        className={`block font-medium text-gray-800 ${hideLabel ? 'sr-only' : isCompact ? 'text-xs mb-1' : 'text-sm mb-2'}`}
      >
        {label}
        {required && <span className="text-red-500 ml-1">*</span>}
      </label>
      <div className="relative">
        <button
          type="button"
          id={name}
          disabled={disabled || loading}
          onClick={() => setOpen((prev) => !prev)}
          className={`${buttonClass} ${
            error
              ? 'border-red-300 focus:ring-red-500 focus:border-red-500'
              : 'border-gray-300'
          } ${disabled || loading ? 'bg-gray-50 cursor-not-allowed text-gray-500' : 'bg-white hover:border-gray-400'}`}
        >
          {loading ? (
            <span className="text-gray-500">Loading…</span>
          ) : selected ? (
            <span className="block truncate">
              <span className="font-medium text-gray-900">{selected.label}</span>
              {selected.hint && (
                <span className="ml-1 text-xs text-gray-500">· {selected.hint}</span>
              )}
            </span>
          ) : (
            <span className="text-gray-400">{placeholder}</span>
          )}
        </button>

        {open && !disabled && !loading && (
          <div className="absolute z-50 mt-1 w-full min-w-[16rem] rounded-lg border border-gray-200 bg-white shadow-lg ring-1 ring-black/5">
            <div className="p-2 border-b border-gray-100">
              <input
                ref={searchInputRef}
                type="text"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                placeholder="Type to search products…"
                autoFocus
                className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-500/30"
              />
            </div>
            <ul className="max-h-56 overflow-y-auto py-1" role="listbox">
              {filtered.length === 0 ? (
                <li className="px-4 py-3 text-sm text-gray-500">No products found</li>
              ) : (
                filtered.map((option) => (
                  <li key={String(option.value)} role="option">
                    <button
                      type="button"
                      onClick={() => handleSelect(option.value)}
                      className={`w-full px-3 py-2.5 text-left text-sm hover:bg-blue-50 focus:bg-blue-50 focus:outline-none ${
                        String(option.value) === String(value)
                          ? 'bg-blue-50 font-medium text-blue-700'
                          : 'text-gray-800'
                      }`}
                    >
                      <span className="block truncate">{option.label}</span>
                      {option.hint && (
                        <span className="block text-xs text-gray-500 mt-0.5">Base unit: {option.hint}</span>
                      )}
                    </button>
                  </li>
                ))
              )}
            </ul>
          </div>
        )}
      </div>
      {error && (
        <p className={`text-red-600 ${isCompact ? 'mt-1 text-xs' : 'mt-1 text-sm'}`}>{error}</p>
      )}
    </div>
  );
};

export default SearchableSelect;

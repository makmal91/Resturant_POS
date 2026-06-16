import React, { useEffect, useMemo, useRef, useState } from 'react';

export interface SearchableSelectOption {
  label: string;
  value: string | number;
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
}) => {
  const [open, setOpen] = useState(false);
  const [search, setSearch] = useState('');
  const containerRef = useRef<HTMLDivElement>(null);

  const selected = useMemo(
    () => options.find((o) => String(o.value) === String(value)),
    [options, value],
  );

  const filtered = useMemo(() => {
    const term = search.trim().toLowerCase();
    if (!term) return options;
    return options.filter((o) => o.label.toLowerCase().includes(term));
  }, [options, search]);

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setOpen(false);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const handleSelect = (optionValue: string | number) => {
    onChange(name, optionValue);
    setOpen(false);
    setSearch('');
  };

  return (
    <div className="mb-5" ref={containerRef}>
      <label htmlFor={name} className="block text-sm font-medium text-gray-800 mb-2">
        {label}
        {required && <span className="text-red-500 ml-1">*</span>}
      </label>
      <div className="relative">
        <button
          type="button"
          id={name}
          disabled={disabled || loading}
          onClick={() => setOpen((prev) => !prev)}
          className={`w-full px-4 py-3 border rounded-lg shadow-sm text-left focus:outline-none focus:ring-2 focus:ring-primary focus:border-primary transition-colors duration-200 ${
            error
              ? 'border-red-300 focus:ring-red-500 focus:border-red-500'
              : 'border-gray-300 focus:ring-blue-500 focus:border-blue-500'
          } ${disabled || loading ? 'bg-gray-50 cursor-not-allowed text-gray-500' : 'bg-white'}`}
        >
          {loading ? 'Loading…' : selected?.label || placeholder}
        </button>

        {open && !disabled && !loading && (
          <div className="absolute z-50 mt-1 w-full rounded-lg border border-gray-200 bg-white shadow-lg">
            <div className="p-2 border-b border-gray-100">
              <input
                type="text"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                placeholder="Search…"
                autoFocus
                className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
              />
            </div>
            <ul className="max-h-52 overflow-y-auto py-1">
              {filtered.length === 0 ? (
                <li className="px-4 py-2 text-sm text-gray-500">No matches found</li>
              ) : (
                filtered.map((option) => (
                  <li key={String(option.value)}>
                    <button
                      type="button"
                      onClick={() => handleSelect(option.value)}
                      className={`w-full px-4 py-2 text-left text-sm hover:bg-blue-50 ${
                        String(option.value) === String(value) ? 'bg-blue-50 font-medium text-blue-700' : 'text-gray-800'
                      }`}
                    >
                      {option.label}
                    </button>
                  </li>
                ))
              )}
            </ul>
          </div>
        )}
      </div>
      {error && (
        <p className="mt-1 text-sm text-red-600">{error}</p>
      )}
    </div>
  );
};

export default SearchableSelect;

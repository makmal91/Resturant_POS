import React from 'react';

export interface SearchInputProps {
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  inputRef?: React.RefObject<HTMLInputElement | null>;
  shortcutHint?: string;
  className?: string;
}

const SearchInput: React.FC<SearchInputProps> = React.memo(
  ({ value, onChange, placeholder = 'Search…', inputRef, shortcutHint, className = '' }) => (
    <div className={`relative ${className}`}>
      <span className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-gray-400">
        <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
          <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-4.35-4.35M11 18a7 7 0 100-14 7 7 0 000 14z" />
        </svg>
      </span>
      <input
        ref={inputRef}
        type="search"
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder={placeholder}
        className="w-full min-h-[44px] pl-10 pr-3 rounded-lg border border-gray-200 bg-white text-sm text-gray-800 placeholder:text-gray-400 focus:outline-none focus:border-[#0a3c6d] focus:ring-2 focus:ring-[#0a3c6d]/20 transition-shadow duration-75"
      />
      {shortcutHint && !value && (
        <span className="pointer-events-none absolute right-3 top-1/2 -translate-y-1/2 hidden sm:inline text-[10px] font-medium text-gray-400 bg-gray-50 border border-gray-200 rounded px-1.5 py-0.5">
          {shortcutHint}
        </span>
      )}
    </div>
  ),
);

SearchInput.displayName = 'SearchInput';

export default SearchInput;

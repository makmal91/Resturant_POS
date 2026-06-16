import React, { useState } from 'react';
import { FormInput } from './index';
import { CodeModuleName, codeGeneratorService } from '../../services/codeGeneratorService';
import { getApiErrorMessage } from '../../services/api';

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
}

const CodeFieldWithGenerate: React.FC<CodeFieldWithGenerateProps> = ({
  label,
  name,
  value,
  onChange,
  module,
  branchId,
  placeholder = 'Auto-generated if empty',
  error,
  disabled = false,
  required = false,
}) => {
  const [isGenerating, setIsGenerating] = useState(false);
  const [generateError, setGenerateError] = useState('');

  const handleGenerate = async () => {
    setIsGenerating(true);
    setGenerateError('');
    try {
      const code = await codeGeneratorService.generate(module, branchId);
      onChange(code);
    } catch (err) {
      setGenerateError(getApiErrorMessage(err, 'Unable to generate code.'));
    } finally {
      setIsGenerating(false);
    }
  };

  return (
    <div>
      <FormInput
        label={label}
        name={name}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder={placeholder}
        error={error || generateError}
        disabled={disabled}
        required={required}
      />
      {!disabled && (
        <button
          type="button"
          onClick={() => void handleGenerate()}
          disabled={isGenerating}
          className="mb-4 -mt-2 inline-flex items-center rounded-md border border-gray-300 bg-white px-3 py-1.5 text-sm font-medium text-gray-700 shadow-sm hover:bg-gray-50 disabled:cursor-not-allowed disabled:opacity-60"
        >
          {isGenerating ? 'Generating…' : 'Auto Generate'}
        </button>
      )}
    </div>
  );
};

export default CodeFieldWithGenerate;

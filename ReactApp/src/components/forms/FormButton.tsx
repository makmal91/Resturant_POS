import React from 'react';

interface FormButtonProps {
  type?: 'submit' | 'button' | 'reset';
  label: string;
  onClick?: () => void;
  disabled?: boolean;
  loading?: boolean;
  variant?: 'primary' | 'secondary' | 'danger';
  className?: string;
}

const FormButton: React.FC<FormButtonProps> = ({
  type = 'submit',
  label,
  onClick,
  disabled = false,
  loading = false,
  variant = 'primary',
  className = '',
}) => {
  const baseStyles = 'px-6 py-2 rounded-lg font-medium transition focus:outline-none focus:ring-2 focus:ring-offset-2';

  const variantStyles = {
    primary: 'bg-blue-600 text-white hover:bg-blue-700 focus:ring-blue-500 disabled:bg-gray-400',
    secondary: 'bg-gray-300 text-gray-800 hover:bg-gray-400 focus:ring-gray-400 disabled:bg-gray-200',
    danger: 'bg-red-600 text-white hover:bg-red-700 focus:ring-red-500 disabled:bg-gray-400',
  };

  return (
    <button
      type={type}
      onClick={onClick}
      disabled={disabled || loading}
      className={`${baseStyles} ${variantStyles[variant]} ${className}`}
    >
      {loading ? 'Loading...' : label}
    </button>
  );
};

export default FormButton;

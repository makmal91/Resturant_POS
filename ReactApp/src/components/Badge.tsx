import React from 'react';

export interface BadgeProps {
  variant?: 'success' | 'danger' | 'warning' | 'info' | 'primary' | 'secondary';
  size?: 'sm' | 'md' | 'lg';
  children: React.ReactNode;
  rounded?: boolean;
  dot?: boolean;
}

const Badge: React.FC<BadgeProps> = ({
  variant = 'primary',
  size = 'md',
  children,
  rounded = false,
  dot = false,
}) => {
  const baseStyles = 'inline-flex items-center font-medium';

  const variantStyles = {
    success: 'bg-green-100 text-green-800',
    danger: 'bg-red-100 text-red-800',
    warning: 'bg-yellow-100 text-yellow-800',
    info: 'bg-blue-100 text-blue-800',
    primary: 'bg-indigo-100 text-indigo-800',
    secondary: 'bg-gray-100 text-gray-800',
  };

  const sizeStyles = {
    sm: 'px-2.5 py-0.5 text-xs',
    md: 'px-3 py-1 text-sm',
    lg: 'px-4 py-2 text-base',
  };

  const roundedStyles = rounded ? 'rounded-full' : 'rounded';

  return (
    <span className={`${baseStyles} ${variantStyles[variant]} ${sizeStyles[size]} ${roundedStyles}`}>
      {dot && (
        <span className={`inline-block w-2 h-2 rounded-full mr-2 ${
          variant === 'success' ? 'bg-green-600' :
          variant === 'danger' ? 'bg-red-600' :
          variant === 'warning' ? 'bg-yellow-600' :
          variant === 'info' ? 'bg-blue-600' :
          variant === 'primary' ? 'bg-indigo-600' :
          'bg-gray-600'
        }`} />
      )}
      {children}
    </span>
  );
};

export default Badge;
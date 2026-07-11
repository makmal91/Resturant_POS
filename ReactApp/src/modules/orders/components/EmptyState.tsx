import React from 'react';

export interface EmptyStateProps {
  title: string;
  subtitle?: string;
}

const EmptyState: React.FC<EmptyStateProps> = React.memo(({ title, subtitle }) => (
  <div className="flex flex-col items-center justify-center py-16 px-4 text-center">
    <p className="text-sm font-medium text-gray-700">{title}</p>
    {subtitle && <p className="text-xs text-gray-500 mt-1">{subtitle}</p>}
  </div>
));

EmptyState.displayName = 'EmptyState';

export default EmptyState;

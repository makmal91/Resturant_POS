import React from 'react';
import { POS_INTERACTION, POS_THEME } from '../theme';

export interface CategoryButtonProps {
  name: string;
  count: number;
  selected: boolean;
  onClick: () => void;
}

const CategoryButton: React.FC<CategoryButtonProps> = React.memo(
  ({ name, count, selected, onClick }) => (
    <button
      type="button"
      onClick={onClick}
      className={`w-full min-h-[48px] rounded-lg px-3 py-2.5 text-left text-sm font-medium border flex items-center justify-between gap-2 ${POS_INTERACTION.button} ${
        selected
          ? 'text-white border-transparent shadow-sm'
          : 'bg-white text-gray-800 border-gray-200 hover:bg-gray-50'
      }`}
      style={selected ? { backgroundColor: POS_THEME.primary } : undefined}
    >
      <span className="line-clamp-2 leading-snug flex-1 min-w-0">{name}</span>
      <span
        className={`shrink-0 text-xs font-semibold tabular-nums px-2 py-0.5 rounded-md ${
          selected ? 'bg-white/20 text-white' : 'bg-gray-100 text-gray-500'
        }`}
      >
        {count}
      </span>
    </button>
  ),
);

CategoryButton.displayName = 'CategoryButton';

export default CategoryButton;

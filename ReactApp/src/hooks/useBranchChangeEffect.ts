import { useEffect, useRef } from 'react';
import { useBranchStore } from '../stores/useBranchStore';

/**
 * Re-runs callback when the global branch selection changes (header selector).
 */
export const useBranchChangeEffect = (callback: () => void | Promise<void>): void => {
  const selectedBranchId = useBranchStore((state) => state.selectedBranchId);
  const callbackRef = useRef(callback);
  callbackRef.current = callback;

  useEffect(() => {
    void callbackRef.current();
  }, [selectedBranchId]);
};

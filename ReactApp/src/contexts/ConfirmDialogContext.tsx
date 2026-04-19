import React, { createContext, useContext, useState } from 'react';

interface ConfirmDialogContextType {
  isOpen: boolean;
  title: string;
  message: string;
  confirmLabel: string;
  cancelLabel: string;
  isLoading: boolean;
  showConfirm: (options: {
    title: string;
    message: string;
    confirmLabel?: string;
    cancelLabel?: string;
    onConfirm: () => void | Promise<void>;
  }) => void;
  closeConfirm: () => void;
  handleConfirm: () => Promise<void>;
}

const ConfirmDialogContext = createContext<ConfirmDialogContextType | undefined>(undefined);

export const ConfirmDialogProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [isOpen, setIsOpen] = useState(false);
  const [title, setTitle] = useState('');
  const [message, setMessage] = useState('');
  const [confirmLabel, setConfirmLabel] = useState('Confirm');
  const [cancelLabel, setCancelLabel] = useState('Cancel');
  const [isLoading, setIsLoading] = useState(false);
  const [onConfirmCallback, setOnConfirmCallback] = useState<(() => void | Promise<void>) | null>(null);

  const showConfirm = (options: {
    title: string;
    message: string;
    confirmLabel?: string;
    cancelLabel?: string;
    onConfirm: () => void | Promise<void>;
  }) => {
    setTitle(options.title);
    setMessage(options.message);
    setConfirmLabel(options.confirmLabel || 'Confirm');
    setCancelLabel(options.cancelLabel || 'Cancel');
    setOnConfirmCallback(() => options.onConfirm);
    setIsOpen(true);
  };

  const closeConfirm = () => {
    setIsOpen(false);
    setOnConfirmCallback(null);
    setIsLoading(false);
  };

  const handleConfirm = async () => {
    if (onConfirmCallback) {
      setIsLoading(true);
      try {
        await onConfirmCallback();
      } catch (error) {
        console.error('Confirmation action failed:', error);
      } finally {
        setIsLoading(false);
        closeConfirm();
      }
    }
  };

  return (
    <ConfirmDialogContext.Provider
      value={{
        isOpen,
        title,
        message,
        confirmLabel,
        cancelLabel,
        isLoading,
        showConfirm,
        closeConfirm,
        handleConfirm,
      }}
    >
      {children}
    </ConfirmDialogContext.Provider>
  );
};

export const useConfirmDialog = () => {
  const context = useContext(ConfirmDialogContext);
  if (!context) {
    throw new Error('useConfirmDialog must be used within ConfirmDialogProvider');
  }
  return context;
};

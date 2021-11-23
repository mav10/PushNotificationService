import { useState } from 'react';

/**
 * Boilerplate hook for handling visibility of modals.
 */
export const useModal = (defaultState?: 'OPEN' | 'CLOSED'): UseModalProps => {
  const [visible, setIsVisible] = useState(defaultState === 'OPEN');

  const openModal = () => {
    setIsVisible(true);
  };

  const closeModal = () => {
    setIsVisible(false);
  };

  return { visible, openModal, closeModal };
};

export type UseModalProps = {
  visible: boolean;
  openModal: () => void;
  closeModal: () => void;
};

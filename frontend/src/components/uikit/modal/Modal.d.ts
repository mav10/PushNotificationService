import React from 'react';

export type AppModalProps = {
  title: string;
  visible: boolean;
  onHide: () => void;
  isClickOutside?: boolean;
  containerClassName?: string;
  bodyClassName?: any;
  header?: React.ReactElement;
  footer?: React.ReactElement;
};

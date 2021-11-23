import React, { FC } from 'react';
import Style from './Modal.module.scss';
import { AppModalProps } from './Modal';
import { ReactComponent as CrossIcon } from './../../../assets/icons/cross.svg';
import clsx from 'clsx';
import { Modal } from '@fluentui/react';
import { TypographyStyles } from 'styles';

export const AppModalContainer: FC<AppModalProps> = (props) => {
  return (
    <Modal
      scrollableContentClassName={Style.scrollableContent}
      isClickableOutsideFocusTrap={props.isClickOutside ?? true}
      onDismiss={props.onHide}
      isOpen={props.visible}
      containerClassName={clsx(Style.container, props.containerClassName)}
    >
      <div className={Style.header}>
        <div className={Style.headerWrapper}>
          <div className={TypographyStyles.heading1}>{props.title}</div>
          {props.header}
        </div>
        <button onClick={props.onHide} className={Style.closeButton}>
          <CrossIcon className={Style.closIcon} />
        </button>
      </div>
      <div className={clsx(Style.body, props.bodyClassName)}>
        {props.children}
      </div>
      {props.footer && <div className={Style.footer}>{props.footer}</div>}
    </Modal>
  );
};

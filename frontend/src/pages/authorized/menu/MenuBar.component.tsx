import { AppLink } from 'components/uikit/buttons/AppLink';
import React from 'react';
import { Links } from '../../../application/constants/links';
import { MenuBarItemProps } from './MenuBar';
import { ButtonColor } from '../../../components/uikit/buttons/Button';

const styles = require('./MenuBar.module.scss');

const MenuItem = React.memo((props: MenuBarItemProps) => {
  return (
    <AppLink
      color={ButtonColor.Danger}
      to={props.navLink}
      className={styles.item}
    >
      {props.text}
    </AppLink>
  );
});

export const MenuBarComponent = () => {
  return (
    <>
      {Object.keys(Links.Authorized).map((x, index) => {
        // @ts-ignore
        return <MenuItem key={index} navLink={Links.Authorized[x]} text={x} />;
      })}
    </>
  );
};

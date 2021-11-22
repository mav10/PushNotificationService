import clsx from 'clsx';
import { convertToErrorString } from 'helpers/form/useErrorHandler';
import React from 'react';
import { JLottie } from 'components/animations/JLottie';

const animationData = require('assets/animations/9629-loading.json');
const styles = require('./Loading.module.scss');

const defaultOptions = {
  loop: true,
  autoplay: true,
  animationData: JSON.stringify(animationData),
  rendererSettings: {
    preserveAspectRatio: 'xMidYMid slice',
  },
};

interface Props {
  loading: boolean;
  title?: string;
  containerId?: string;
  children?: React.ReactNode;
  className?: string;
  flex?: boolean;
  error?: any;
  wrapperClassName?: string;
  centerContent?: boolean;
}

export const Loading: React.FC<Props> = (props) => {
  const loadingStyles = [props.className];
  loadingStyles.push(styles.loadingContainer);

  return (
    <div
      className={clsx(
        props.flex ? styles.flexContainer : styles.container,
        props.centerContent !== false ? styles.centerContent : null,
        props.wrapperClassName,
      )}
    >
      <div
        data-loaded={props.loading}
        className={props.flex ? styles.flexLoadingData : styles.loadingData}
      >
        {props.error ? (
          <h1 className={styles.loading} data-test-id="loading-error">
            {convertToErrorString(props.error)}
          </h1>
        ) : (
          props.children
        )}
      </div>
      {props.loading && !props.error && (
        <div className={clsx(loadingStyles)} data-test-id="loading">
          <div className={styles.loading}>
            <div className={styles.row}>
              <JLottie options={defaultOptions} className={styles.lottie} />
              {props.title && (
                <div className={styles.title}> {props.title}</div>
              )}
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

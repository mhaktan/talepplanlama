import React from 'react';

type TkProps = Record<string, unknown> & {
  children?: React.ReactNode;
  style?: React.CSSProperties;
  className?: string;
};

declare global {
  namespace JSX {
    interface IntrinsicElements {
      'tk-card': TkProps;
      'tk-alert': TkProps;
      'tk-button': TkProps;
      'tk-tabs': TkProps;
      'tk-tabs-item': TkProps;
    }
  }
}

export {};

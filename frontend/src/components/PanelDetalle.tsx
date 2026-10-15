import type { ReactNode } from 'react';

interface PanelDetalleProps {
  ariaLabelledBy: string;
  className: string;
  children: ReactNode;
}

export function PanelDetalle({ ariaLabelledBy, className, children }: PanelDetalleProps) {
  return (
    <section aria-labelledby={ariaLabelledBy} className={className}>
      {children}
    </section>
  );
}

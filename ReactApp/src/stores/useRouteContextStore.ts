import { create } from 'zustand';

interface RouteContextState {
  module: string | null;
  form: string | null;
  setRouteContext: (module: string | null, form: string | null) => void;
  clearRouteContext: () => void;
}

export const useRouteContextStore = create<RouteContextState>((set) => ({
  module: null,
  form: null,
  setRouteContext: (module, form) => set({ module, form }),
  clearRouteContext: () => set({ module: null, form: null }),
}));

import { useEffect } from 'react';
import { useLocation } from 'react-router-dom';
import { resolveRouteContext } from '../routeRegistry';
import { useRouteContextStore } from '../stores/useRouteContextStore';

const RouteContextSync = () => {
  const location = useLocation();
  const setRouteContext = useRouteContextStore((state) => state.setRouteContext);

  useEffect(() => {
    const { module, form } = resolveRouteContext(location.pathname);
    setRouteContext(module, form);
  }, [location.pathname, setRouteContext]);

  return null;
};

export default RouteContextSync;

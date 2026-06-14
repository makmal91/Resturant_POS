import React from 'react';
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import Layout from './components/Layout';
import FormModal from './components/FormModal';
import ConfirmDialog from './components/ConfirmDialog';
import { FormModalProvider } from './contexts/FormModalContext';
import { ConfirmDialogProvider } from './contexts/ConfirmDialogContext';
import { navigationItems } from './navigationConfig';
import { useTenantStore } from './stores/useTenantStore';

function App() {
  const hydrateTenant = useTenantStore((state) => state.hydrate);

  React.useEffect(() => {
    hydrateTenant();
  }, [hydrateTenant]);

  return (
    <Router>
      <FormModalProvider>
        <ConfirmDialogProvider>
          <Layout>
            <Routes>
              {navigationItems.map((item) => (
                <Route key={item.path} path={item.path} element={<item.component />} />
              ))}
            </Routes>
            <FormModal />
            <ConfirmDialog />
          </Layout>
        </ConfirmDialogProvider>
      </FormModalProvider>
    </Router>
  );
}

export default App;
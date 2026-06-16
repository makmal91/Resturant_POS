import React from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import Layout from './components/Layout';
import FormModal from './components/FormModal';
import ConfirmDialog from './components/ConfirmDialog';
import ProtectedRoute from './components/ProtectedRoute';
import { FormModalProvider } from './contexts/FormModalContext';
import { ConfirmDialogProvider } from './contexts/ConfirmDialogContext';
import { AuthProvider } from './contexts/AuthContext';
import { routeRegistry } from './routeRegistry';
import LoginPage from './pages/LoginPage';
import BranchSelectionPage from './pages/BranchSelectionPage';
import POSBillingPage from './modules/pos/POSBillingPage';
import EditInvoicePage from './modules/sales/EditInvoicePage';
import OpeningCashPage from './modules/cashflow/OpeningCashPage';
import ClosingCashPage from './modules/cashflow/ClosingCashPage';

function App() {
  return (
    <AuthProvider>
      <Router>
        <FormModalProvider>
          <ConfirmDialogProvider>
            <Routes>
              <Route path="/login" element={<LoginPage />} />
              <Route
                path="/select-branch"
                element={
                  <ProtectedRoute requireBranch={false}>
                    <BranchSelectionPage />
                  </ProtectedRoute>
                }
              />
              {/* POS Billing — fullscreen, outside the sidebar/header Layout */}
              <Route
                path="/pos"
                element={
                  <ProtectedRoute module="POS Billing">
                    <POSBillingPage />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/*"
                element={
                  <ProtectedRoute>
                    <Layout>
                      <Routes>
                        {routeRegistry.map((item) => (
                          <Route
                            key={item.path}
                            path={item.path}
                            element={
                              item.module ? (
                                <ProtectedRoute module={item.module}>
                                  <item.component />
                                </ProtectedRoute>
                              ) : (
                                <item.component />
                              )
                            }
                          />
                        ))}
                        {/* Edit invoice — dynamic route, not in sidebar */}
                        <Route
                          path="/sales-invoices/edit/:id"
                          element={
                            <ProtectedRoute module="Sales">
                              <EditInvoicePage />
                            </ProtectedRoute>
                          }
                        />
                        {/* Cash Flow — form pages, not in sidebar */}
                        <Route
                          path="/cashflow/opening"
                          element={
                            <ProtectedRoute module="Cash Flow">
                              <OpeningCashPage />
                            </ProtectedRoute>
                          }
                        />
                        <Route
                          path="/cashflow/closing"
                          element={
                            <ProtectedRoute module="Cash Flow">
                              <ClosingCashPage />
                            </ProtectedRoute>
                          }
                        />
                        <Route path="*" element={<Navigate to="/" replace />} />
                      </Routes>
                      <FormModal />
                      <ConfirmDialog />
                    </Layout>
                  </ProtectedRoute>
                }
              />
            </Routes>
          </ConfirmDialogProvider>
        </FormModalProvider>
      </Router>
    </AuthProvider>
  );
}

export default App;

import React from 'react';
import { Link } from 'react-router-dom';
import { useIsMasterUser, usePermission } from '../../hooks/usePermission';

const settingsLinks = [
  {
    title: 'System License',
    description: 'Upload and monitor the signed system license, expiry, and usage limits.',
    path: '/settings/licenses',
    masterOnly: true,
  },
  {
    title: 'Code Sequences',
    description: 'View and manage auto-numbering sequences for products, customers, invoices, and more.',
    path: '/settings/code-sequences',
    module: 'Code Sequences',
  },
];

const SettingsPage: React.FC = () => {
  const codeSeqPerm = usePermission('Code Sequences');
  const isMasterUser = useIsMasterUser();

  const visibleLinks = settingsLinks.filter((link) => {
    if (link.masterOnly) return isMasterUser;
    if (link.module === 'Code Sequences') return codeSeqPerm.canView;
    return true;
  });

  return (
    <div className="p-6">
      <div className="mb-8">
        <h1 className="text-2xl font-bold text-gray-900">System Settings</h1>
        <p className="mt-1 text-sm text-gray-600">
          Configure master data and system-wide preferences.
        </p>
      </div>

      {visibleLinks.length === 0 ? (
        <div className="rounded-lg border border-dashed border-gray-300 bg-gray-50 p-8 text-center text-sm text-gray-600">
          No settings modules are available for your role.
        </div>
      ) : (
        <div className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-3">
          {visibleLinks.map((link) => (
            <Link
              key={link.path}
              to={link.path}
              className="rounded-xl border border-gray-200 bg-white p-5 shadow-sm transition hover:border-blue-300 hover:shadow-md"
            >
              <h2 className="text-lg font-semibold text-gray-900">{link.title}</h2>
              <p className="mt-2 text-sm text-gray-600">{link.description}</p>
            </Link>
          ))}
        </div>
      )}
    </div>
  );
};

export default SettingsPage;

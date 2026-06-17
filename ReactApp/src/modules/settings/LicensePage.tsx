import React, { useCallback, useEffect, useRef, useState } from 'react';
import { Navigate } from 'react-router-dom';
import { useIsMasterUser } from '../../hooks/usePermission';
import { getApiErrorMessage } from '../../services/api';
import {
  licenseService,
  LicenseStatus,
  LicenseUsage,
} from './licenseService';

const formatDate = (value?: string | null) => {
  if (!value) return '—';
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? value : date.toLocaleString();
};

const statusBadgeClass = (status: LicenseStatus) => {
  if (!status.isValid) return 'bg-red-100 text-red-800 border-red-200';
  if (status.isExpired) return 'bg-amber-100 text-amber-800 border-amber-200';
  return 'bg-green-100 text-green-800 border-green-200';
};

const statusLabel = (status: LicenseStatus) => {
  if (!status.isValid) return 'Invalid';
  if (status.isExpired) return 'Expired';
  return 'Active';
};

const LicensePage: React.FC = () => {
  const isMasterUser = useIsMasterUser();
  const fileInputRef = useRef<HTMLInputElement | null>(null);

  const [status, setStatus] = useState<LicenseStatus | null>(null);
  const [usage, setUsage] = useState<LicenseUsage | null>(null);
  const [loading, setLoading] = useState(true);
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  const loadData = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const [nextStatus, nextUsage] = await Promise.all([
        licenseService.getStatus(),
        licenseService.getUsage(),
      ]);
      setStatus(nextStatus);
      setUsage(nextUsage);
    } catch (err) {
      setError(getApiErrorMessage(err, 'Failed to load license information.'));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (isMasterUser) {
      void loadData();
    }
  }, [isMasterUser, loadData]);

  if (!isMasterUser) {
    return <Navigate to="/settings" replace />;
  }

  const handleUpload = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    event.target.value = '';
    if (!file) return;

    setUploading(true);
    setError('');
    setSuccess('');
    try {
      const result = await licenseService.uploadLicense(file);
      setStatus(result.licenseStatus);
      setUsage(result.usage);
      setSuccess(result.message);
    } catch (err) {
      setError(getApiErrorMessage(err, 'Failed to upload license file.'));
    } finally {
      setUploading(false);
    }
  };

  const handleReload = async () => {
    setError('');
    setSuccess('');
    try {
      const result = await licenseService.reloadLicense();
      setStatus(result.licenseStatus);
      setSuccess(result.message);
      const nextUsage = await licenseService.getUsage();
      setUsage(nextUsage);
    } catch (err) {
      setError(getApiErrorMessage(err, 'Failed to reload license.'));
    }
  };

  return (
    <div className="p-6">
      <div className="mb-6 flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">System License</h1>
          <p className="mt-1 text-sm text-gray-600">
            Upload a signed `.lic` file to update limits instantly without restarting the server.
          </p>
        </div>

        <div className="flex flex-wrap gap-2">
          <button
            type="button"
            onClick={() => fileInputRef.current?.click()}
            disabled={uploading}
            className="rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 disabled:opacity-60"
          >
            {uploading ? 'Uploading…' : 'Upload License File'}
          </button>
          <button
            type="button"
            onClick={() => void handleReload()}
            className="rounded-lg border border-gray-300 px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50"
          >
            Reload Cache
          </button>
          <input
            ref={fileInputRef}
            type="file"
            accept=".lic"
            className="hidden"
            onChange={(event) => void handleUpload(event)}
          />
        </div>
      </div>

      {error && (
        <div className="mb-4 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
          {error}
        </div>
      )}
      {success && (
        <div className="mb-4 rounded-lg border border-green-200 bg-green-50 px-4 py-3 text-sm text-green-700">
          {success}
        </div>
      )}

      {loading ? (
        <div className="rounded-lg border border-gray-200 bg-white p-8 text-center text-sm text-gray-600">
          Loading license details…
        </div>
      ) : (
        <>
          <div className="mb-6 grid grid-cols-1 gap-4 xl:grid-cols-3">
            <div className="rounded-xl border border-gray-200 bg-white p-5 shadow-sm xl:col-span-2">
              <div className="mb-4 flex items-center justify-between gap-3">
                <h2 className="text-lg font-semibold text-gray-900">Current License</h2>
                {status && (
                  <span className={`rounded-full border px-3 py-1 text-xs font-semibold ${statusBadgeClass(status)}`}>
                    {statusLabel(status)}
                  </span>
                )}
              </div>

              <dl className="grid grid-cols-1 gap-4 md:grid-cols-2">
                <div>
                  <dt className="text-xs font-medium uppercase tracking-wide text-gray-500">Customer</dt>
                  <dd className="mt-1 text-sm text-gray-900">{status?.customerName || '—'}</dd>
                </div>
                <div>
                  <dt className="text-xs font-medium uppercase tracking-wide text-gray-500">License ID</dt>
                  <dd className="mt-1 break-all font-mono text-sm text-gray-900">{status?.licenseId || '—'}</dd>
                </div>
                <div>
                  <dt className="text-xs font-medium uppercase tracking-wide text-gray-500">Issued</dt>
                  <dd className="mt-1 text-sm text-gray-900">{formatDate(status?.issuedAt)}</dd>
                </div>
                <div>
                  <dt className="text-xs font-medium uppercase tracking-wide text-gray-500">Expires</dt>
                  <dd className="mt-1 text-sm text-gray-900">{formatDate(status?.expiresAt)}</dd>
                </div>
                <div>
                  <dt className="text-xs font-medium uppercase tracking-wide text-gray-500">Loaded At</dt>
                  <dd className="mt-1 text-sm text-gray-900">{formatDate(status?.loadedAt)}</dd>
                </div>
                <div>
                  <dt className="text-xs font-medium uppercase tracking-wide text-gray-500">Status Message</dt>
                  <dd className="mt-1 text-sm text-gray-900">{status?.message || 'License is active.'}</dd>
                </div>
              </dl>
            </div>

            <div className="rounded-xl border border-gray-200 bg-white p-5 shadow-sm">
              <h2 className="mb-4 text-lg font-semibold text-gray-900">Limits</h2>
              <div className="space-y-4">
                <div>
                  <div className="flex items-center justify-between text-sm">
                    <span className="text-gray-600">Businesses</span>
                    <span className="font-medium text-gray-900">
                      {usage?.currentBusinesses ?? 0} / {status?.maxBusinesses ?? 0}
                    </span>
                  </div>
                </div>
                <div>
                  <div className="flex items-center justify-between text-sm">
                    <span className="text-gray-600">Users</span>
                    <span className="font-medium text-gray-900">
                      {usage?.totalUsers ?? 0} / {status?.maxUsers ?? 0}
                    </span>
                  </div>
                </div>
                <div>
                  <div className="text-sm text-gray-600">Branches per business</div>
                  <div className="mt-1 text-sm font-medium text-gray-900">
                    Max {status?.maxBranchesPerBusiness ?? 0}
                  </div>
                </div>
              </div>
            </div>
          </div>

          <div className="rounded-xl border border-gray-200 bg-white p-5 shadow-sm">
            <h2 className="mb-4 text-lg font-semibold text-gray-900">Branch Usage by Business</h2>
            {(usage?.branchUsageByBusiness.length ?? 0) === 0 ? (
              <p className="text-sm text-gray-600">No businesses found.</p>
            ) : (
              <div className="overflow-x-auto">
                <table className="min-w-full divide-y divide-gray-200 text-sm">
                  <thead className="bg-gray-50">
                    <tr>
                      <th className="px-4 py-3 text-left font-medium text-gray-600">Business</th>
                      <th className="px-4 py-3 text-left font-medium text-gray-600">Branches Used</th>
                      <th className="px-4 py-3 text-left font-medium text-gray-600">Limit</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-gray-100">
                    {usage?.branchUsageByBusiness.map((item) => (
                      <tr key={item.businessId}>
                        <td className="px-4 py-3 text-gray-900">{item.businessName}</td>
                        <td className="px-4 py-3 text-gray-900">{item.currentBranches}</td>
                        <td className="px-4 py-3 text-gray-900">{item.maxBranchesPerBusiness}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </>
      )}
    </div>
  );
};

export default LicensePage;

import { useEffect, useMemo, useState } from 'react';
import { useAuth } from '../../../contexts/AuthContext';
import { hasBranchContext } from '../../../types/permissions';
import { warehouseService, type WarehouseItem } from '../../warehouse/warehouseService';
import { customerService } from '../../customer/customerService';
import type { PosCustomer } from '../posService';

export function usePOSBillingSetup() {
  const { user, selectedBranchId } = useAuth();
  const branchId: number = selectedBranchId ?? (user as { branchId?: number })?.branchId ?? 1;
  const businessId: number = (user as { businessId?: number })?.businessId ?? 1;

  const [warehouses, setWarehouses] = useState<WarehouseItem[]>([]);
  const [warehouseId, setWarehouseId] = useState<number>(0);
  const [pricingType, setPricingType] = useState<'Retail' | 'Wholesale'>('Retail');
  const [customer, setCustomer] = useState<PosCustomer | null>(null);
  const [walkInCustomer, setWalkInCustomer] = useState<PosCustomer | null>(null);
  const [error, setError] = useState('');

  const effectiveBranchId = useMemo(() => {
    if (branchId > 0) return branchId;
    const selectedWarehouse = warehouses.find((w) => w.id === warehouseId);
    return selectedWarehouse?.branchId ?? 0;
  }, [branchId, warehouseId, warehouses]);

  useEffect(() => {
    if (!hasBranchContext(branchId)) return;
    warehouseService
      .getAllActive(branchId)
      .then((r) => {
        const rows = Array.isArray(r.data) ? r.data : [];
        setWarehouses(rows);
        if (rows.length > 0) setWarehouseId(rows[0].id);
      })
      .catch(() => setError('Failed to load warehouses.'));
  }, [branchId]);

  useEffect(() => {
    const loadBranchId = effectiveBranchId > 0 ? effectiveBranchId : branchId > 0 ? branchId : 0;
    if (!hasBranchContext(loadBranchId) || loadBranchId <= 0) return;
    customerService
      .getWalkIn(loadBranchId)
      .then((r) => {
        const wi: PosCustomer = {
          id: r.data.id,
          name: r.data.name,
          phone: r.data.phone ?? '',
          email: r.data.email ?? '',
        };
        setWalkInCustomer(wi);
        setCustomer(wi);
      })
      .catch(() => {
        /* walk-in may not exist yet */
      });
  }, [branchId, effectiveBranchId]);

  const resetCustomer = () => {
    setCustomer(walkInCustomer);
  };

  return {
    user,
    branchId,
    businessId,
    effectiveBranchId,
    warehouses,
    warehouseId,
    setWarehouseId,
    pricingType,
    setPricingType,
    customer,
    setCustomer,
    walkInCustomer,
    resetCustomer,
    error,
    setError,
  };
}

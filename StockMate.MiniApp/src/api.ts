import type { Assignment, Customer, Order, PagedResult, Product, Shipment, ShipmentStatus, Staff } from './types';

const baseUrl = (import.meta.env.VITE_API_URL as string | undefined)?.replace(/\/$/, '') ?? 'http://localhost:5157';

function initData() { return window.Telegram?.WebApp.initData ?? ''; }

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const response = await fetch(`${baseUrl}${path}`, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      'X-Telegram-Init-Data': initData(),
      ...options.headers,
    },
  });
  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { detail?: string } | null;
    throw new Error(problem?.detail ?? 'Не удалось выполнить операцию.');
  }
  if (response.status === 204) return undefined as T;
  return response.json() as Promise<T>;
}

export const api = {
  authenticate: () => request<Staff>('/api/miniapp/auth', { method: 'POST', body: JSON.stringify({ initData: initData() }) }),
  getNewOrders: () => request<Order[]>('/api/miniapp/admin/orders/new'),
  getActiveOrders: () => request<Order[]>('/api/miniapp/admin/orders/active'),
  getOrderHistory: () => request<Order[]>('/api/miniapp/admin/orders/history'),
  getOrder: (number: number) => request<Order>(`/api/miniapp/admin/orders/${number}`),
  confirmOrder: (number: number) => request<void>(`/api/miniapp/admin/orders/${number}/confirm`, { method: 'PUT' }),
  setDeliveryMode: (number: number, mode: 'AllTogether' | 'PartialAllowed') => request<void>(`/api/miniapp/admin/orders/${number}/delivery-mode`, { method: 'PUT', body: JSON.stringify({ mode }) }),
  cancelOrder: (number: number, reason: string) => request<void>(`/api/miniapp/admin/orders/${number}/cancel`, { method: 'PUT', body: JSON.stringify({ reason }) }),
  getStaff: () => request<Staff[]>('/api/miniapp/admin/staff'),
  createWorker: (data: { telegramUserId: number; firstName: string; lastName?: string; username?: string }) => request<Staff>('/api/miniapp/admin/staff/workers', { method: 'POST', body: JSON.stringify(data) }),
  setWorkerActive: (id: string, active: boolean) => request<void>(`/api/miniapp/admin/staff/${id}/${active ? 'activate' : 'deactivate'}`, { method: 'PUT' }),
  getProducts: (search = '') => request<PagedResult<Product>>(`/api/miniapp/admin/products?pageSize=100&search=${encodeURIComponent(search)}`),
  changeStock: (id: string, action: 'add' | 'remove' | 'set', quantity: number, reason: string) => request<void>(`/api/miniapp/admin/products/${id}/stock`, { method: 'POST', body: JSON.stringify({ action, quantity, reason }) }),
  getCustomers: () => request<Customer[]>('/api/miniapp/admin/customers'),
  getAssignment: (number: number) => request<Assignment | null>(`/api/miniapp/admin/orders/${number}/assignment`),
  assignOrder: (number: number, workerId: string) => request<Assignment>(`/api/miniapp/admin/orders/${number}/assign`, { method: 'POST', body: JSON.stringify({ workerId }) }),
  getShipments: (number: number) => request<Shipment[]>(`/api/miniapp/admin/orders/${number}/shipments`),
  createShipment: (number: number, workerId: string) => request<Shipment>(`/api/miniapp/admin/orders/${number}/shipments`, { method: 'POST', body: JSON.stringify({ workerId }) }),
  getWorkerShipments: () => request<Shipment[]>('/api/miniapp/worker/shipments'),
  changeShipmentStatus: (id: string, status: ShipmentStatus) => request<Shipment>(`/api/miniapp/shipments/${id}/status`, { method: 'PUT', body: JSON.stringify({ status }) }),
};

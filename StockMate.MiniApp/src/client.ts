import type {
  Assignment,
  Customer,
  CreateStockPurchaseItem,
  DeliveryCompany,
  DeliveryMode,
  InventoryTurnoverReport,
  MissingProduct,
  MissingProductOrder,
  Order,
  PagedResult,
  Product,
  ProductInput,
  Shipment,
  ShipmentStatus,
  SpecialOrderStatus,
  Staff,
  StockAction,
  StockMovement,
  StockPurchase,
} from './models';

const configuredBaseUrl = (import.meta.env.VITE_API_URL as string | undefined)?.trim();
const baseUrl = configuredBaseUrl ? configuredBaseUrl.replace(/\/$/, '') : '';

function initData() {
  return window.Telegram?.WebApp.initData ?? '';
}

async function readError(response: Response) {
  const problem = await response.json().catch(() => null) as { detail?: string; title?: string } | null;
  return problem?.detail ?? problem?.title ?? 'Əməliyyatı yerinə yetirmək mümkün olmadı.';
}

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const response = await fetch(`${baseUrl}${path}`, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      'X-Telegram-Init-Data': initData(),
      ...options.headers,
    },
  });
  if (!response.ok) throw new Error(await readError(response));
  if (response.status === 204) return undefined as T;
  return response.json() as Promise<T>;
}

async function getAllProducts(search = '') {
  const first = await request<PagedResult<Product>>(`/api/miniapp/admin/products?pageNumber=1&pageSize=20&search=${encodeURIComponent(search)}`);
  const products = [...first.items];
  for (let page = 2; page <= first.totalPages; page += 1) {
    const next = await request<PagedResult<Product>>(`/api/miniapp/admin/products?pageNumber=${page}&pageSize=20&search=${encodeURIComponent(search)}`);
    products.push(...next.items);
  }
  return products;
}

export const productImageUrl = (productId: string) => `${baseUrl}/api/product-images/${productId}`;

export const api = {
  authenticate: () => request<Staff>('/api/miniapp/auth', {
    method: 'POST', body: JSON.stringify({ initData: initData() }),
  }),
  getNewOrders: () => request<Order[]>('/api/miniapp/admin/orders/new'),
  getActiveOrders: () => request<Order[]>('/api/miniapp/admin/orders/active'),
  getOrderHistory: () => request<Order[]>('/api/miniapp/admin/orders/history'),
  getOrder: (number: number) => request<Order>(`/api/miniapp/admin/orders/${number}`),
  confirmOrder: (number: number) => request<void>(`/api/miniapp/admin/orders/${number}/confirm`, { method: 'PUT' }),
  markPreparing: (number: number) => request<void>(`/api/miniapp/admin/orders/${number}/preparing`, { method: 'PUT' }),
  markDelivering: (number: number) => request<void>(`/api/miniapp/admin/orders/${number}/delivering`, { method: 'PUT' }),
  completeOrder: (number: number) => request<void>(`/api/miniapp/admin/orders/${number}/complete`, { method: 'PUT' }),
  deliverReady: (number: number) => request<Order>(`/api/miniapp/admin/orders/${number}/deliver-ready`, { method: 'PUT' }),
  cancelRemaining: (number: number) => request<Order>(`/api/miniapp/admin/orders/${number}/cancel-remaining`, { method: 'PUT' }),
  setDeliveryMode: (number: number, mode: DeliveryMode) => request<void>(`/api/miniapp/admin/orders/${number}/delivery-mode`, { method: 'PUT', body: JSON.stringify({ mode }) }),
  setDeliveryFee: (number: number, deliveryFee: number) => request<void>(`/api/miniapp/admin/orders/${number}/delivery-fee`, { method: 'PUT', body: JSON.stringify({ deliveryFee }) }),
  setAdminNote: (number: number, adminNote?: string) => request<void>(`/api/miniapp/admin/orders/${number}/admin-note`, { method: 'PUT', body: JSON.stringify({ adminNote: adminNote || null }) }),
  changeSpecialStatus: (number: number, itemId: string, status: SpecialOrderStatus) => request<void>(`/api/miniapp/admin/orders/${number}/items/${itemId}/special-status`, { method: 'PUT', body: JSON.stringify({ status }) }),
  cancelOrder: (number: number, reason: string) => request<void>(`/api/miniapp/admin/orders/${number}/cancel`, { method: 'PUT', body: JSON.stringify({ reason }) }),
  getStaff: () => request<Staff[]>('/api/miniapp/admin/staff'),
  createWorker: (data: { telegramUserId: number; firstName: string; lastName?: string; username?: string }) => request<Staff>('/api/miniapp/admin/staff/workers', { method: 'POST', body: JSON.stringify(data) }),
  setWorkerActive: (id: string, active: boolean) => request<void>(`/api/miniapp/admin/staff/${id}/${active ? 'activate' : 'deactivate'}`, { method: 'PUT' }),
  getProducts: (pageNumber = 1, search = '') => request<PagedResult<Product>>(`/api/miniapp/admin/products?pageNumber=${pageNumber}&pageSize=20&search=${encodeURIComponent(search)}`),
  getAllProducts,
  createProduct: (data: ProductInput) => request<Product>('/api/miniapp/admin/products', { method: 'POST', body: JSON.stringify(data) }),
  updateProduct: (id: string, data: { name: string; description: string; imageReference?: string }) => request<void>(`/api/miniapp/admin/products/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
  changePrice: (id: string, price: number) => request<void>(`/api/miniapp/admin/products/${id}/price`, { method: 'PATCH', body: JSON.stringify({ price }) }),
  setProductActive: (id: string, isActive: boolean) => request<void>(`/api/miniapp/admin/products/${id}/active`, { method: 'PUT', body: JSON.stringify({ isActive }) }),
  changeStock: (id: string, action: StockAction, quantity: number, reason: string) => request<void>(`/api/miniapp/admin/products/${id}/stock`, { method: 'POST', body: JSON.stringify({ action, quantity, reason }) }),
  getStockHistory: (id: string) => request<StockMovement[]>(`/api/miniapp/admin/products/${id}/stock-history`),
  uploadProductImage: async (image: File) => {
    const form = new FormData();
    form.append('image', image);
    const response = await fetch(`${baseUrl}/api/miniapp/admin/products/images`, {
      method: 'POST', headers: { 'X-Telegram-Init-Data': initData() }, body: form,
    });
    if (!response.ok) throw new Error(await readError(response));
    return response.json() as Promise<{ imageReference: string }>;
  },
  getCustomers: () => request<Customer[]>('/api/miniapp/admin/customers'),
  getCustomerOrders: (id: string) => request<Order[]>(`/api/miniapp/admin/customers/${id}/orders`),
  getAssignment: (number: number) => request<Assignment | null>(`/api/miniapp/admin/orders/${number}/assignment`),
  getWorkerAssignments: () => request<Assignment[]>('/api/miniapp/worker/assignments'),
  assignOrder: (number: number, workerId: string) => request<Assignment>(`/api/miniapp/admin/orders/${number}/assign`, { method: 'POST', body: JSON.stringify({ workerId }) }),
  releaseAssignment: (number: number) => request<void>(`/api/miniapp/admin/orders/${number}/assignment`, { method: 'DELETE' }),
  getShipments: (number: number) => request<Shipment[]>(`/api/miniapp/admin/orders/${number}/shipments`),
  createShipment: (number: number, workerId: string) => request<Shipment>(`/api/miniapp/admin/orders/${number}/shipments`, { method: 'POST', body: JSON.stringify({ workerId }) }),
  getWorkerShipments: () => request<Shipment[]>('/api/miniapp/worker/shipments'),
  changeShipmentStatus: (id: string, status: ShipmentStatus) => request<Shipment>(`/api/miniapp/shipments/${id}/status`, { method: 'PUT', body: JSON.stringify({ status }) }),
  getShortages: () => request<MissingProduct[]>('/api/miniapp/admin/shortages'),
  getShortageOrders: (productId: string) => request<MissingProductOrder[]>(`/api/miniapp/admin/shortages/${productId}/orders`),
  getStockPurchases: (fromUtc: string, toUtc: string) => request<StockPurchase[]>(`/api/miniapp/admin/stock-purchases?fromUtc=${encodeURIComponent(fromUtc)}&toUtc=${encodeURIComponent(toUtc)}`),
  createStockPurchase: (purchasedAtUtc: string, note: string | undefined, items: CreateStockPurchaseItem[]) => request<StockPurchase>('/api/miniapp/admin/stock-purchases', { method: 'POST', body: JSON.stringify({ purchasedAtUtc, note: note || null, items }) }),
  getTurnoverReport: (fromUtc: string, toUtc: string, deliveryCompanyId?: string) => request<InventoryTurnoverReport>(`/api/miniapp/admin/reports/inventory-turnover?fromUtc=${encodeURIComponent(fromUtc)}&toUtc=${encodeURIComponent(toUtc)}${deliveryCompanyId ? `&deliveryCompanyId=${encodeURIComponent(deliveryCompanyId)}` : ''}`),
  getDeliveryCompanies: () => request<DeliveryCompany[]>('/api/miniapp/admin/delivery-companies'),
  createDeliveryCompany: (name: string) => request<DeliveryCompany>('/api/miniapp/admin/delivery-companies', { method: 'POST', body: JSON.stringify({ name }) }),
  renameDeliveryCompany: (id: string, name: string) => request<DeliveryCompany>(`/api/miniapp/admin/delivery-companies/${id}/name`, { method: 'PUT', body: JSON.stringify({ name }) }),
  setDeliveryCompanyActive: (id: string, active: boolean) => request<void>(`/api/miniapp/admin/delivery-companies/${id}/${active ? 'activate' : 'deactivate'}`, { method: 'PUT' }),
};

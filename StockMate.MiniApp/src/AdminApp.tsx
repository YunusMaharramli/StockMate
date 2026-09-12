import { useEffect, useMemo, useState, type FormEvent } from 'react';
import { api, productImageUrl } from './client';
import CustomerApp from './CustomerApp';
import type {
  Assignment,
  Category,
  CreateStockPurchaseItem,
  Customer,
  DeliveryCompany,
  DeliveryMode,
  InventoryTurnoverReport,
  MissingProduct,
  MissingProductOrder,
  MiniAppIdentity,
  Order,
  OrderItem,
  OrderStatus,
  Product,
  ProductInput,
  Shipment,
  ShipmentStatus,
  SpecialOrderStatus,
  Staff,
  StockAction,
  StockMovement,
  StockPurchase,
  Subcategory,
} from './models';

type Section = 'home' | 'orders' | 'products' | 'stock' | 'shortages' | 'purchases' | 'reports' | 'companies' | 'staff' | 'customers' | 'invites' | 'history';
type OrderFilter = 'new' | 'active' | 'partial' | 'special' | 'completed' | 'cancelled';

const now = new Date().toISOString();
const defaultToday = now.slice(0, 10);
const defaultFromDate = new Date(now);
defaultFromDate.setDate(defaultFromDate.getDate() - 30);
const defaultFrom = defaultFromDate.toISOString().slice(0, 10);
const nowDate = new Date(now);
const defaultLocalDateTime = new Date(nowDate.getTime() - nowDate.getTimezoneOffset() * 60_000).toISOString().slice(0, 16);
const demoCustomers: Customer[] = [
  { id: 'c1', telegramUserId: 123456789, firstName: 'Yunus', lastName: 'Məhərrəmli', username: 'yunus', phoneNumber: '077 329 08 78', deliveryAddress: 'Nərimanov, Bakı', productPageSize: 10, isBlocked: false },
  { id: 'c2', telegramUserId: 987654321, firstName: 'Aysel', lastName: 'Əliyeva', phoneNumber: '050 555 11 22', deliveryAddress: 'Yasamal, Bakı', productPageSize: 10, isBlocked: false },
];
const demoStaff: Staff[] = [
  { id: 'a1', telegramUserId: 111111, firstName: 'Admin', role: 'Admin', isActive: true },
  { id: 'w1', telegramUserId: 222222, firstName: 'Elvin', lastName: 'Əliyev', username: 'elvin', role: 'Worker', isActive: true },
  { id: 'w2', telegramUserId: 333333, firstName: 'Nigar', lastName: 'Həsənli', role: 'Worker', isActive: false },
];
const demoProducts: Product[] = [
  { id: 'p1', name: 'Coca-Cola 1 L', code: 'CC-1L', price: 1.8, stockQuantity: 12, description: 'Qazlı sərinləşdirici içki', isActive: true, isArchived: false, categoryId: 'cat1', categoryName: 'Coca-Cola', subcategoryId: 'sub1', subcategoryName: '1 L', minimumStockQuantity: 5, isLowStock: false, isCatalogVisible: true },
  { id: 'p2', name: 'Fanta 1 L', code: 'FN-1L', price: 2, stockQuantity: 4, description: 'Portağal dadlı içki', isActive: true, isArchived: false, categoryId: 'cat2', categoryName: 'Fanta', subcategoryId: 'sub2', subcategoryName: '1 L', minimumStockQuantity: 5, isLowStock: true, isCatalogVisible: true },
  { id: 'p3', name: 'Su 500 ml', code: 'SU-05', price: 0.7, stockQuantity: 0, description: 'Qazsız içməli su', isActive: false, isArchived: false, categoryId: 'cat3', categoryName: 'Su', minimumStockQuantity: 5, isLowStock: true, isCatalogVisible: false },
];
const demoCategories: Category[] = [
  { id: 'cat1', name: 'Coca-Cola', isActive: true, isArchived: false, sortOrder: 0, subcategoryCount: 1, productCount: 1 },
  { id: 'cat2', name: 'Fanta', isActive: true, isArchived: false, sortOrder: 1, subcategoryCount: 1, productCount: 1 },
  { id: 'cat3', name: 'Su', isActive: true, isArchived: false, sortOrder: 2, subcategoryCount: 0, productCount: 1 },
];
const demoSubcategories: Subcategory[] = [
  { id: 'sub1', categoryId: 'cat1', categoryName: 'Coca-Cola', name: '1 L', isActive: true, isArchived: false, sortOrder: 0, productCount: 1 },
  { id: 'sub2', categoryId: 'cat2', categoryName: 'Fanta', name: '1 L', isActive: true, isArchived: false, sortOrder: 0, productCount: 1 },
];
const demoItems: OrderItem[] = [
  { id: 'i1', productId: 'p1', productName: 'Coca-Cola 1 L', productCode: 'CC-1L', unitPrice: 1.8, fromStockQuantity: 2, specialOrderQuantity: 0, deliveredQuantity: 0, cancelledQuantity: 0, remainingQuantity: 2, readyQuantity: 2, totalQuantity: 2, lineTotal: 3.6, specialOrderStatus: 'NotRequired' },
];
const demoOrders: Order[] = [
  { id: 'o1', orderNumber: 1048, customerId: 'c1', phoneNumber: '077 329 08 78', deliveryAddress: 'Nərimanov, Bakı', status: 'PendingAdminApproval', deliveryMode: 'Undecided', items: demoItems, productsTotal: 3.6, totalAmount: 3.6, containsSpecialOrder: false, fulfillmentStatus: 'Waiting', remainingQuantity: 2, readyQuantity: 2, createdAtUtc: now },
  { id: 'o2', orderNumber: 1046, customerId: 'c2', phoneNumber: '050 555 11 22', deliveryAddress: 'Yasamal, Bakı', status: 'Preparing', deliveryMode: 'PartialAllowed', items: [{ id: 'i2', productId: 'p2', productName: 'Fanta 1 L', productCode: 'FN-1L', unitPrice: 2, fromStockQuantity: 3, specialOrderQuantity: 2, deliveredQuantity: 0, cancelledQuantity: 0, remainingQuantity: 5, readyQuantity: 3, totalQuantity: 5, lineTotal: 10, specialOrderStatus: 'Pending' }], productsTotal: 10, totalAmount: 10, containsSpecialOrder: true, fulfillmentStatus: 'Waiting', remainingQuantity: 5, readyQuantity: 3, createdAtUtc: now },
];
const demoShipments: Shipment[] = [
  { id: 's1', orderNumber: 1046, staffId: 'w1', staffName: 'Elvin Əliyev', status: 'Assigned', phoneNumber: '050 555 11 22', deliveryAddress: 'Yasamal, Bakı', items: [{ orderItemId: 'i2', productName: 'Fanta 1 L', quantity: 3 }], createdAtUtc: now },
];

const orderLabels: Record<OrderStatus, string> = {
  PendingAdminApproval: 'Yeni', Confirmed: 'Təsdiqlənib', Preparing: 'Hazırlanır', Delivering: 'Çatdırılmada', Completed: 'Tamamlanıb', Cancelled: 'Ləğv edilib',
};
const shipmentLabels: Record<ShipmentStatus, string> = {
  Assigned: 'Təyin edilib', Preparing: 'Hazırlanır', Ready: 'Hazırdır', OutForDelivery: 'Çatdırılmada', Delivered: 'Çatdırılıb', Cancelled: 'Ləğv edilib',
};
const specialLabels: Record<SpecialOrderStatus, string> = {
  NotRequired: 'Anbardan', Pending: 'Gözləyir', OrderedFromSupplier: 'Sifariş verilib', Arrived: 'Gəlib', Completed: 'Tamamlanıb', Cancelled: 'Ləğv edilib',
};
const nextShipment: Partial<Record<ShipmentStatus, ShipmentStatus>> = {
  Assigned: 'Preparing', Preparing: 'Ready', Ready: 'OutForDelivery', OutForDelivery: 'Delivered',
};

function money(value?: number) { return `${(value ?? 0).toFixed(2)} AZN`; }
function date(value?: string) { return value ? new Intl.DateTimeFormat('az-AZ', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value)) : '—'; }
function inputDate(value: Date) { return value.toISOString().slice(0, 10); }
function periodBounds(from: string, to: string) {
  const start = new Date(`${from}T00:00:00`);
  const end = new Date(`${to}T00:00:00`);
  end.setDate(end.getDate() + 1);
  return [start.toISOString(), end.toISOString()] as const;
}
function fullName(value?: Pick<Customer | Staff, 'firstName' | 'lastName'>) { return value ? [value.firstName, value.lastName].filter(Boolean).join(' ') : 'Naməlum müştəri'; }
function filterOrder(order: Order, filter: OrderFilter) {
  if (filter === 'new') return order.status === 'PendingAdminApproval';
  if (filter === 'active') return ['Confirmed', 'Preparing', 'Delivering'].includes(order.status);
  if (filter === 'partial') return order.fulfillmentStatus === 'PartiallyDelivered' || (order.readyQuantity < order.remainingQuantity && order.status !== 'Cancelled');
  if (filter === 'special') return order.containsSpecialOrder && order.status !== 'Cancelled';
  if (filter === 'completed') return order.status === 'Completed';
  return order.status === 'Cancelled';
}

export default function AdminApp() {
  const [identity, setIdentity] = useState<MiniAppIdentity | null>(null);
  const [demo, setDemo] = useState(false);
  const [section, setSection] = useState<Section>('home');
  const [orders, setOrders] = useState<Order[]>(demoOrders);
  const [products, setProducts] = useState<Product[]>(demoProducts);
  const [categories, setCategories] = useState<Category[]>(demoCategories);
  const [subcategories, setSubcategories] = useState<Subcategory[]>(demoSubcategories);
  const [staff, setStaff] = useState<Staff[]>(demoStaff);
  const [customers, setCustomers] = useState<Customer[]>(demoCustomers);
  const [workerAssignments, setWorkerAssignments] = useState<Assignment[]>([]);
  const [workerShipments, setWorkerShipments] = useState<Shipment[]>(demoShipments);
  const [shortages, setShortages] = useState<MissingProduct[]>([]);
  const [stockPurchases, setStockPurchases] = useState<StockPurchase[]>([]);
  const [deliveryCompanies, setDeliveryCompanies] = useState<DeliveryCompany[]>([]);
  const [turnoverReport, setTurnoverReport] = useState<InventoryTurnoverReport | null>(null);
  const [selectedOrder, setSelectedOrder] = useState<Order | null>(null);
  const [assignment, setAssignment] = useState<Assignment | null>(null);
  const [shipments, setShipments] = useState<Shipment[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');

  const demoRole = new URLSearchParams(window.location.search).get('role');
  const demoWorker = demoRole === 'worker';
  const demoCustomer = demoRole === 'customer';
  const isWorker = identity?.role === 'Worker' || (demo && demoWorker);
  const isCustomer = identity?.role === 'Customer' || (demo && demoCustomer);

  useEffect(() => {
    const webApp = window.Telegram?.WebApp;
    const handleAccessDenied = (event: Event) => {
      const message = event instanceof CustomEvent && typeof event.detail === 'string'
        ? event.detail
        : 'İcazəniz yoxdur.';

      setIdentity(null);
      setError(message);
      setLoading(false);
    };

    window.addEventListener(
      'stockmate:access-denied',
      handleAccessDenied,
    );

    webApp?.ready();
    webApp?.expand();
    if (webApp) document.documentElement.dataset.telegramTheme = webApp.colorScheme;
    if (!webApp?.initData) {
      if (import.meta.env.DEV) {
        setDemo(true);
      } else {
        setError(
          'İcazəniz yoxdur. Mini App-ı Telegram botundakı düymədən açın.',
        );
      }
      setLoading(false);
      return () => window.removeEventListener(
        'stockmate:access-denied',
        handleAccessDenied,
      );
    }
    api.authenticate()
      .then(async user => {
        setIdentity(user);
        if (user.role === 'Worker') {
          const [assigned, deliveries] = await Promise.all([
            api.getWorkerAssignments(),
            api.getWorkerShipments(),
          ]);
          setWorkerAssignments(assigned);
          setWorkerShipments(deliveries);
        }
        else if (user.role === 'Admin') {
          const [newOrders, activeOrders, history, people, customerList] = await Promise.all([api.getNewOrders(), api.getActiveOrders(), api.getOrderHistory(), api.getStaff(), api.getCustomers()]);
          setOrders([...newOrders, ...activeOrders, ...history]);
          setStaff(people);
          setCustomers(customerList);
        }
      })
      .catch(reason => setError(reason instanceof Error ? reason.message : 'Giriş alınmadı.'))
      .finally(() => setLoading(false));

    return () => window.removeEventListener(
      'stockmate:access-denied',
      handleAccessDenied,
    );
  }, []);

  useEffect(() => {
    if (demo || identity?.role !== 'Worker') return;

    let disposed = false;
    const refresh = async () => {
      try {
        const [assigned, deliveries] = await Promise.all([
          api.getWorkerAssignments(),
          api.getWorkerShipments(),
        ]);
        if (!disposed) {
          setWorkerAssignments(assigned);
          setWorkerShipments(deliveries);
        }
      } catch (reason) {
        if (!disposed) setError(reason instanceof Error ? reason.message : 'Tapşırıqlar yenilənmədi.');
      }
    };
    const onVisibilityChange = () => {
      if (document.visibilityState === 'visible') void refresh();
    };
    const timer = window.setInterval(() => void refresh(), 10_000);
    document.addEventListener('visibilitychange', onVisibilityChange);
    return () => {
      disposed = true;
      window.clearInterval(timer);
      document.removeEventListener('visibilitychange', onVisibilityChange);
    };
  }, [demo, identity?.role]);

  async function run(action: () => Promise<void>, message: string) {
    setLoading(true); setError(''); setNotice('');
    try { await action(); setNotice(message); return true; }
    catch (reason) { setError(reason instanceof Error ? reason.message : 'Əməliyyat alınmadı.'); return false; }
    finally { setLoading(false); }
  }

  async function refreshOrders() {
    if (demo) return;
    const [fresh, active, history] = await Promise.all([api.getNewOrders(), api.getActiveOrders(), api.getOrderHistory()]);
    setOrders([...fresh, ...active, ...history]);
  }

  async function refreshCatalog() {
    if (demo) return;
    const [catalog, categoryList, subcategoryList] = await Promise.all([
      api.getAllProducts(),
      api.getCategories(true),
      api.getSubcategories(undefined, true),
    ]);
    setProducts(catalog);
    setCategories(categoryList);
    setSubcategories(subcategoryList);
  }

  async function openSection(next: Section) {
    setSection(next); setSelectedOrder(null); setError('');
    if (demo || identity?.role !== 'Admin') return;
    await run(async () => {
      if (next === 'products') {
        const [catalog, categoryList, subcategoryList] = await Promise.all([
          api.getAllProducts(),
          api.getCategories(true),
          api.getSubcategories(undefined, true),
        ]);
        setProducts(catalog);
        setCategories(categoryList);
        setSubcategories(subcategoryList);
      }
      if (next === 'stock') setProducts(await api.getAllProducts());
      if (next === 'shortages') setShortages(await api.getShortages());
      if (next === 'purchases') {
        const [fromUtc, toUtc] = periodBounds(inputDate(new Date(Date.now() - 30 * 86_400_000)), inputDate(new Date()));
        const [items, catalog] = await Promise.all([api.getStockPurchases(fromUtc, toUtc), api.getAllProducts()]);
        setStockPurchases(items); setProducts(catalog);
      }
      if (next === 'reports') setDeliveryCompanies(await api.getDeliveryCompanies());
      if (next === 'companies') setDeliveryCompanies(await api.getDeliveryCompanies());
      if (next === 'staff') setStaff(await api.getStaff());
      if (next === 'customers') setCustomers(await api.getCustomers());
      if (next === 'orders' || next === 'history' || next === 'home') {
        const [, customerList] = await Promise.all([
          refreshOrders(),
          api.getCustomers(),
        ]);
        setCustomers(customerList);
      }
    }, 'Məlumatlar yeniləndi.');
  }

  async function openOrder(order: Order) {
    setSelectedOrder(order); setAssignment(null); setShipments([]);
    if (demo) {
      if (order.orderNumber === 1046) {
        setAssignment({ id: 'as1', orderNumber: 1046, staffId: 'w1', staffName: 'Elvin Əliyev', assignedAtUtc: now });
        setShipments(workerShipments.filter(item => item.orderNumber === order.orderNumber));
      }
      return;
    }
    await run(async () => {
      const [detail, assigned, delivery] = await Promise.all([api.getOrder(order.orderNumber), api.getAssignment(order.orderNumber), api.getShipments(order.orderNumber)]);
      setSelectedOrder(detail); setAssignment(assigned); setShipments(delivery);
    }, 'Sifariş açıldı.');
  }

  function updateDemoOrder(number: number, changes: Partial<Order>) {
    setOrders(values => values.map(value => value.orderNumber === number ? { ...value, ...changes } : value));
    setSelectedOrder(value => value?.orderNumber === number ? { ...value, ...changes } : value);
  }

  async function orderAction(action: string, value?: string) {
    if (!selectedOrder) return;
    const number = selectedOrder.orderNumber;
    await run(async () => {
      if (demo) {
        if (action === 'confirm') updateDemoOrder(number, { status: 'Confirmed', confirmedAtUtc: now });
        if (action === 'preparing') updateDemoOrder(number, { status: 'Preparing' });
        if (action === 'delivering') updateDemoOrder(number, { status: 'Delivering' });
        if (action === 'complete') updateDemoOrder(number, { status: 'Completed', fulfillmentStatus: 'FullyDelivered', remainingQuantity: 0, completedAtUtc: now });
        if (action === 'cancel') updateDemoOrder(number, { status: 'Cancelled', cancellationReason: value || 'Admin tərəfindən ləğv edildi', cancelledAtUtc: now });
        if (action === 'mode') updateDemoOrder(number, { deliveryMode: value as DeliveryMode });
        if (action === 'fee') updateDemoOrder(number, { deliveryFee: Number(value), totalAmount: selectedOrder.productsTotal + Number(value) });
        if (action === 'note') updateDemoOrder(number, { adminNote: value });
        if (action === 'assign') {
          const worker = staff.find(item => item.id === value)!;
          const assigned = { id: crypto.randomUUID(), orderNumber: number, staffId: worker.id, staffName: fullName(worker), assignedAtUtc: now };
          setAssignment(assigned);
          setWorkerAssignments(items => [...items.filter(item => item.orderNumber !== number), assigned]);
          if (selectedOrder.readyQuantity > 0) {
            const created: Shipment = { id: crypto.randomUUID(), orderNumber: number, staffId: assigned.staffId, staffName: assigned.staffName, status: 'Assigned', phoneNumber: selectedOrder.phoneNumber, deliveryAddress: selectedOrder.deliveryAddress, items: selectedOrder.items.filter(item => item.readyQuantity > 0).map(item => ({ orderItemId: item.id, productName: item.productName, quantity: item.readyQuantity })), createdAtUtc: now };
            setShipments(items => [...items, created]);
            setWorkerShipments(items => [...items, created]);
          }
        }
        if (action === 'release') setAssignment(null);
        if (action === 'shipment' && assignment) {
          const created: Shipment = { id: crypto.randomUUID(), orderNumber: number, staffId: assignment.staffId, staffName: assignment.staffName, status: 'Assigned', phoneNumber: selectedOrder.phoneNumber, deliveryAddress: selectedOrder.deliveryAddress, items: selectedOrder.items.filter(item => item.readyQuantity > 0).map(item => ({ orderItemId: item.id, productName: item.productName, quantity: item.readyQuantity })), createdAtUtc: now };
          setShipments(items => [...items, created]); setWorkerShipments(items => [...items, created]);
        }
        if (action === 'shipmentStatus' && value) {
          const [id, status] = value.split('|') as [string, ShipmentStatus];
          setShipments(items => items.map(item => item.id === id ? { ...item, status } : item));
          setWorkerShipments(items => items.map(item => item.id === id ? { ...item, status } : item));
          if (status === 'Delivered') {
            const deliveredShipment = shipments.find(item => item.id === id);
            if (deliveredShipment) {
              const deliveredByItem = new Map(deliveredShipment.items.map(item => [item.orderItemId, item.quantity]));
              const items = selectedOrder.items.map(item => {
                const deliveredNow = deliveredByItem.get(item.id) ?? 0;
                return {
                  ...item,
                  deliveredQuantity: item.deliveredQuantity + deliveredNow,
                  readyQuantity: Math.max(0, item.readyQuantity - deliveredNow),
                  remainingQuantity: Math.max(0, item.remainingQuantity - deliveredNow),
                };
              });
              const remainingQuantity = items.reduce((sum, item) => sum + item.remainingQuantity, 0);
              const deliveredQuantity = items.reduce((sum, item) => sum + item.deliveredQuantity, 0);
              updateDemoOrder(number, {
                items,
                remainingQuantity,
                readyQuantity: items.reduce((sum, item) => sum + item.readyQuantity, 0),
                fulfillmentStatus: remainingQuantity === 0 ? 'FullyDelivered' : deliveredQuantity > 0 ? 'PartiallyDelivered' : 'Waiting',
                status: remainingQuantity === 0 ? 'Completed' : 'Preparing',
                completedAtUtc: remainingQuantity === 0 ? now : undefined,
              });
            }
          }
        }
        if (action === 'special' && value) {
          const [id, status] = value.split('|') as [string, SpecialOrderStatus];
          const items = selectedOrder.items.map(item => item.id === id ? { ...item, specialOrderStatus: status, readyQuantity: status === 'Arrived' || status === 'Completed' ? item.totalQuantity - item.deliveredQuantity : item.readyQuantity } : item);
          updateDemoOrder(number, { items, readyQuantity: items.reduce((sum, item) => sum + item.readyQuantity, 0) });
        }
        return;
      }
      if (action === 'confirm') await api.confirmOrder(number);
      if (action === 'preparing') await api.markPreparing(number);
      if (action === 'delivering') await api.markDelivering(number);
      if (action === 'complete') await api.completeOrder(number);
      if (action === 'deliverReady') await api.deliverReady(number);
      if (action === 'cancelRemaining') await api.cancelRemaining(number);
      if (action === 'cancel') await api.cancelOrder(number, value || 'Admin tərəfindən ləğv edildi');
      if (action === 'mode') await api.setDeliveryMode(number, value as DeliveryMode);
      if (action === 'fee') await api.setDeliveryFee(number, Number(value));
      if (action === 'note') await api.setAdminNote(number, value);
      if (action === 'special' && value) { const [id, status] = value.split('|'); await api.changeSpecialStatus(number, id, status as SpecialOrderStatus); }
      if (action === 'assign') {
        const assigned = await api.assignOrder(number, value!);
        setAssignment(assigned);
        if (selectedOrder.readyQuantity > 0) {
          const created = await api.createShipment(number, assigned.staffId);
          setShipments(items => [...items, created]);
        }
      }
      if (action === 'release') { await api.releaseAssignment(number); setAssignment(null); }
      if (action === 'shipment' && assignment) {
        const created = await api.createShipment(number, assignment.staffId);
        setShipments(items => [...items, created]);
      }
      if (action === 'shipmentStatus' && value) { const [id, status] = value.split('|'); const updated = await api.changeShipmentStatus(id, status as ShipmentStatus); setShipments(items => items.map(item => item.id === id ? updated : item)); }
      const refreshed = await api.getOrder(number);
      setSelectedOrder(refreshed);
      await refreshOrders();
    }, 'Sifariş yeniləndi.');
  }

  if (loading && !demo && !identity) return <Startup />;
  if (error && !demo && !identity) return <Startup error={error} />;
  if (isCustomer) return <CustomerApp identity={identity} demo={demo} />;
  if (isWorker) return <WorkerPanel assignments={workerAssignments} shipments={workerShipments} loading={loading} demo={demo} error={error} onRefresh={() => run(async () => {
    if (demo) return;
    const [assigned, deliveries] = await Promise.all([api.getWorkerAssignments(), api.getWorkerShipments()]);
    setWorkerAssignments(assigned);
    setWorkerShipments(deliveries);
  }, 'Tapşırıqlar yeniləndi.')} onChange={(shipment, status) => run(async () => {
    if (demo) setWorkerShipments(items => items.map(item => item.id === shipment.id ? { ...item, status } : item));
    else { const updated = await api.changeShipmentStatus(shipment.id, status); setWorkerShipments(items => items.map(item => item.id === updated.id ? updated : item)); }
  }, 'Tapşırığın statusu yeniləndi.')} />;

  return <main className="app-shell">
    <Header demo={demo} subtitle="Admin paneli" />
    <Messages error={error} notice={notice} clear={() => { setError(''); setNotice(''); }} />
    {section === 'home' && <Home orders={orders} products={products} staff={staff} open={openSection} />}
    {(section === 'orders' || section === 'history') && !selectedOrder && <OrdersScreen orders={orders} customers={customers} history={section === 'history'} back={() => openSection('home')} open={openOrder} />}
    {selectedOrder && <OrderDetail order={selectedOrder} customer={customers.find(item => item.id === selectedOrder.customerId)} workers={staff.filter(item => item.role === 'Worker' && item.isActive)} assignment={assignment} shipments={shipments} back={() => setSelectedOrder(null)} action={orderAction} />}
    {section === 'products' && <ProductsScreen products={products} categories={categories} subcategories={subcategories} demo={demo} back={() => openSection('home')} busy={loading} reload={refreshCatalog} save={(product, data, image) => run(async () => {
      let imageReference = data.imageReference;
      if (!demo && image) imageReference = (await api.uploadProductImage(image)).imageReference;
      if (demo) {
        if (product) setProducts(items => items.map(item => item.id === product.id ? { ...item, ...data, imageReference } : item));
        else setProducts(items => [...items, { ...data, id: crypto.randomUUID(), imageReference, stockQuantity: data.initialStockQuantity, isActive: true, isArchived: false, categoryName: categories.find(item => item.id === data.categoryId)?.name, subcategoryName: subcategories.find(item => item.id === data.subcategoryId)?.name, isLowStock: data.initialStockQuantity <= data.minimumStockQuantity, isCatalogVisible: true }]);
      } else if (product) {
        await api.updateProduct(product.id, { name: data.name, description: data.description, imageReference, categoryId: data.categoryId, subcategoryId: data.subcategoryId, minimumStockQuantity: data.minimumStockQuantity });
        if (product.price !== data.price) await api.changePrice(product.id, data.price);
        await refreshCatalog();
      } else {
        await api.createProduct({ ...data, imageReference });
        await refreshCatalog();
      }
    }, product ? 'Məhsul yeniləndi.' : 'Məhsul əlavə edildi.')} toggle={product => run(async () => {
      if (!demo) await api.setProductActive(product.id, !product.isActive);
      setProducts(items => items.map(item => item.id === product.id ? { ...item, isActive: !item.isActive } : item));
    }, product.isActive ? 'Məhsul kataloqdan çıxarıldı.' : 'Məhsul aktiv edildi.')} />}
    {section === 'stock' && <StockScreen products={products} demo={demo} back={() => openSection('home')} change={(product, action, quantity, reason) => run(async () => {
      if (!demo) await api.changeStock(product.id, action, quantity, reason);
      setProducts(items => items.map(item => item.id === product.id ? { ...item, stockQuantity: action === 'set' ? quantity : item.stockQuantity + (action === 'add' ? quantity : -quantity) } : item));
    }, 'Stok yeniləndi.')} history={async product => demo ? [] : api.getStockHistory(product.id)} />}
    {section === 'shortages' && <ShortagesScreen items={shortages} demo={demo} back={() => openSection('home')} loadOrders={productId => demo ? Promise.resolve([]) : api.getShortageOrders(productId)} exportExcel={() => demo ? Promise.resolve() : api.exportShortages()} openOrderNumber={async number => {
      const order = demo ? orders.find(item => item.orderNumber === number) : await api.getOrder(number);
      if (order) { setSection('orders'); await openOrder(order); }
    }} notify={message => setNotice(message)} />}
    {section === 'purchases' && <StockPurchasesScreen items={stockPurchases} products={products} demo={demo} back={() => openSection('home')} search={(from, to) => run(async () => {
      const [fromUtc, toUtc] = periodBounds(from, to);
      setStockPurchases(demo ? stockPurchases : await api.getStockPurchases(fromUtc, toUtc));
    }, 'Alış tarixçəsi yeniləndi.')} create={async (purchasedAtUtc, note, items) => run(async () => {
      const created = demo ? { id: crypto.randomUUID(), purchasedAtUtc, note, items: items.map((item, index) => { const product = products.find(value => value.id === item.productId)!; return { id: `${index}`, ...item, productName: product.name, productCode: product.code, lineTotal: item.quantity * item.unitCost }; }), totalAmount: items.reduce((sum, item) => sum + item.quantity * item.unitCost, 0), createdAtUtc: now } : await api.createStockPurchase(purchasedAtUtc, note, items);
      setStockPurchases(values => [created, ...values]);
      if (!demo) setProducts(await api.getAllProducts());
    }, 'Stok alışı qeydə alındı.')} />}
    {section === 'reports' && <ReportsScreen report={turnoverReport} companies={deliveryCompanies} back={() => openSection('home')} generate={(from, to, companyId) => run(async () => {
      const [fromUtc, toUtc] = periodBounds(from, to);
      setTurnoverReport(demo ? demoTurnoverReport(fromUtc, toUtc, companyId, deliveryCompanies) : await api.getTurnoverReport(fromUtc, toUtc, companyId));
    }, 'Hesabat hazırlandı.')} />}
    {section === 'companies' && <DeliveryCompaniesScreen items={deliveryCompanies} back={() => openSection('home')} add={name => run(async () => {
      const created = demo ? { id: crypto.randomUUID(), name, isActive: true, createdAtUtc: now } : await api.createDeliveryCompany(name);
      setDeliveryCompanies(values => [...values, created]);
    }, 'Çatdırılma şirkəti əlavə edildi.')} rename={(company, name) => run(async () => {
      const updated = demo ? { ...company, name, updatedAtUtc: now } : await api.renameDeliveryCompany(company.id, name);
      setDeliveryCompanies(values => values.map(value => value.id === company.id ? updated : value));
    }, 'Şirkətin adı yeniləndi.')} toggle={company => run(async () => {
      if (!demo) await api.setDeliveryCompanyActive(company.id, !company.isActive);
      setDeliveryCompanies(values => values.map(value => value.id === company.id ? { ...value, isActive: !value.isActive } : value));
    }, company.isActive ? 'Şirkət passiv edildi.' : 'Şirkət aktiv edildi.')} />}
    {section === 'staff' && <StaffScreen staff={staff} back={() => openSection('home')} add={data => run(async () => {
      const created = demo ? { id: crypto.randomUUID(), role: 'Worker' as const, isActive: true, ...data } : await api.createWorker(data);
      setStaff(items => [...items, created]);
    }, 'İşçi əlavə edildi.')} toggle={worker => run(async () => {
      if (!demo) await api.setWorkerActive(worker.id, !worker.isActive);
      setStaff(items => items.map(item => item.id === worker.id ? { ...item, isActive: !item.isActive } : item));
    }, 'İşçi statusu yeniləndi.')} />}
    {section === 'customers' && <CustomersScreen customers={customers} orders={orders} demo={demo} back={() => openSection('home')} loadOrders={id => demo ? Promise.resolve(orders.filter(item => item.customerId === id)) : api.getCustomerOrders(id)} block={customer => run(async () => {
      if (!demo) await api.blockCustomer(customer.id);
      setCustomers(items => items.map(item => item.id === customer.id ? { ...item, isBlocked: true, blockedAtUtc: now } : item));
    }, 'Müştərinin bot girişi bağlandı. Yeni dəvət linki ilə yenidən aktivləşəcək.')} />}
    {section === 'invites' && <InvitesScreen demo={demo} back={() => openSection('home')} create={async () => demo ? `https://t.me/StockMateAzBot?start=${crypto.randomUUID().replaceAll('-', '')}` : (await api.createCustomerInvite()).url} />}
    <BottomNav section={section} open={openSection} />
    {loading && <div className="busy"><span className="spinner" /></div>}
  </main>;
}

function Startup({ error }: { error?: string }) { return <div className="startup"><span className={error ? 'startup-icon' : 'spinner'}>{error ? '⚠️' : ''}</span><strong>{error ? 'Giriş mümkün olmadı' : 'StockMate açılır…'}</strong>{error && <p>{error}</p>}</div>; }
function Header({ demo, subtitle }: { demo: boolean; subtitle: string }) { return <header className="app-header"><div className="brand">S</div><div className="brand-copy"><strong>StockMate</strong><span>{subtitle}</span></div><span className="connection">{demo ? 'Demo' : 'Telegram'}</span></header>; }
function Messages({ error, notice, clear }: { error: string; notice: string; clear: () => void }) { return error || notice ? <div className={`toast ${error ? 'error' : ''}`} role="status"><span>{error || notice}</span><button onClick={clear}>×</button></div> : null; }
function Heading({ title, back, action }: { title: string; back: () => void; action?: React.ReactNode }) { return <div className="page-heading"><button className="back" onClick={back}>‹</button><div><span>İdarəetmə</span><h1>{title}</h1></div>{action}</div>; }
function Empty({ text }: { text: string }) { return <div className="empty"><span>📭</span><strong>{text}</strong></div>; }

function Home({ orders, products, staff, open }: { orders: Order[]; products: Product[]; staff: Staff[]; open: (value: Section) => void }) {
  const cards: Array<[Section, string, string, string, number]> = [
    ['orders', '📦', 'Sifarişlər', 'Bütün sifariş axını', orders.filter(item => !['Completed', 'Cancelled'].includes(item.status)).length],
    ['products', '🛍️', 'Məhsullar', 'Kataloq və qiymətlər', products.length],
    ['stock', '📊', 'Stok', 'Anbar və hərəkətlər', products.filter(item => item.stockQuantity < 5).length],
    ['shortages', '⚠️', 'Çatışmazlıqlar', 'Sifarişlər üçün çatmayan mallar', 0],
    ['purchases', '🧾', 'Stok alışları', 'Mal qəbulu və alış tarixçəsi', 0],
    ['reports', '📈', 'Hesabatlar', 'Satış və dövriyyə analizi', 0],
    ['companies', '🚚', 'Çatdırılma şirkətləri', 'Şirkətləri idarə et', 0],
    ['staff', '👷', 'İşçilər', 'Komanda və statuslar', staff.filter(item => item.role === 'Worker' && item.isActive).length],
    ['customers', '👥', 'Müştərilər', 'Profil və sifarişlər', 0],
    ['invites', '🔗', 'Dəvət linkləri', 'Yeni müştəri üçün birdəfəlik link', 0],
    ['history', '📜', 'Tarixçə', 'Tamamlanan və ləğv edilən', orders.filter(item => ['Completed', 'Cancelled'].includes(item.status)).length],
  ];
  return <section className="screen"><div className="hero"><span>İdarəetmə mərkəzi</span><h1>Bu gün nəyi idarə edirik?</h1><p>Sifariş, stok, məhsul və işçilər bir yerdə.</p></div><div className="dashboard-grid">{cards.map(([id, icon, title, subtitle, count]) => <button key={id} className="dashboard-card" onClick={() => open(id)}><span className="card-icon">{icon}</span><span><strong>{title}</strong><small>{subtitle}</small></span>{count > 0 && <b>{count}</b>}<em>›</em></button>)}</div></section>;
}

function OrdersScreen({ orders, customers, history, back, open }: { orders: Order[]; customers: Customer[]; history: boolean; back: () => void; open: (order: Order) => void }) {
  const [filter, setFilter] = useState<OrderFilter>(history ? 'completed' : 'new');
  const [search, setSearch] = useState('');
  const filters: OrderFilter[] = history ? ['completed', 'cancelled'] : ['new', 'active', 'partial', 'special'];
  const labels: Record<OrderFilter, string> = { new: 'Yeni', active: 'Aktiv', partial: 'Natamam', special: 'Çatışmayan', completed: 'Tamam', cancelled: 'Ləğv' };
  const searchText = search.trim().toLocaleLowerCase('az');
  const sectionOrders = orders.filter(item => history
    ? ['Completed', 'Cancelled'].includes(item.status)
    : !['Completed', 'Cancelled'].includes(item.status));
  const list = sectionOrders
    .filter(item => searchText.length > 0 || filterOrder(item, filter))
    .filter(item => {
      if (!searchText) return true;
      const customer = customers.find(value => value.id === item.customerId);
      const searchable = [
        item.orderNumber,
        item.phoneNumber,
        item.deliveryAddress,
        customer?.firstName,
        customer?.lastName,
        customer?.username,
      ].filter(Boolean).join(' ').toLocaleLowerCase('az');
      return searchable.includes(searchText.replace(/^#/, ''));
    });
  return <section className="screen"><Heading title={history ? 'Tarixçə' : 'Sifarişlər'} back={back} /><div className="filters">{filters.map(value => <button key={value} className={filter === value ? 'active' : ''} onClick={() => setFilter(value)}><strong>{orders.filter(item => filterOrder(item, value)).length}</strong><span>{labels[value]}</span></button>)}</div><input className="search" placeholder="№, ad, telefon və ya ünvan ilə axtar" value={search} onChange={event => setSearch(event.target.value)} /><div className="list">{list.length ? list.map(order => { const customer = customers.find(item => item.id === order.customerId); return <button className="list-card" key={order.id} onClick={() => open(order)}><div className="row"><strong>#{order.orderNumber}</strong><Status value={orderLabels[order.status]} tone={order.status === 'Cancelled' ? 'danger' : order.status === 'Completed' ? 'success' : 'warning'} /></div><h3>{fullName(customer)}</h3><p>📍 {order.deliveryAddress}</p><div className="row meta"><span>{date(order.createdAtUtc)}</span><b>{money(order.totalAmount)}</b></div></button>; }) : <Empty text="Axtarışa uyğun sifariş tapılmadı" />}</div></section>;
}

function Status({ value, tone = 'info' }: { value: string; tone?: 'info' | 'warning' | 'success' | 'danger' }) { return <span className={`status ${tone}`}>{value}</span>; }

function OrderDetail({ order, customer, workers, assignment, shipments, back, action }: { order: Order; customer?: Customer; workers: Staff[]; assignment: Assignment | null; shipments: Shipment[]; back: () => void; action: (name: string, value?: string) => Promise<void> }) {
  const [workerId, setWorkerId] = useState(assignment?.staffId ?? '');
  const [fee, setFee] = useState(String(order.deliveryFee ?? 0));
  const [note, setNote] = useState(order.adminNote ?? '');
  const [cancelReason, setCancelReason] = useState('');
  const active = !['Completed', 'Cancelled'].includes(order.status);
  const hasActiveShipment = shipments.some(shipment => !['Delivered', 'Cancelled'].includes(shipment.status));
  const canChangeDeliveryMode = order.items.every(item => item.deliveredQuantity === 0);
  const progressText = order.fulfillmentStatus === 'PartiallyDelivered'
    ? `${order.items.reduce((sum, item) => sum + item.deliveredQuantity, 0)} çatdırılıb · ${order.remainingQuantity} qalıb`
    : `${order.readyQuantity}/${order.remainingQuantity} hazır`;
  return <section className="screen"><Heading title={`Sifariş #${order.orderNumber}`} back={back} /><div className="detail-stack">
    <article className="panel"><div className="row"><div><small>Status</small><h2>{orderLabels[order.status]}</h2></div><Status value={progressText} tone={order.fulfillmentStatus === 'PartiallyDelivered' ? 'warning' : 'info'} /></div><p>🕒 {date(order.createdAtUtc)}</p>{order.cancellationReason && <p className="danger-text">Səbəb: {order.cancellationReason}</p>}</article>
    <article className="panel"><h2>Müştəri və çatdırılma</h2><h3>{fullName(customer)}</h3><a href={`tel:${order.phoneNumber}`}>📞 {order.phoneNumber}</a><p>📍 {order.deliveryAddress}</p>{order.deliveryCompanyName && <p>🚚 {order.deliveryCompanyName}</p>}{order.latitude && order.longitude && <a target="_blank" rel="noreferrer" href={`https://maps.google.com/?q=${order.latitude},${order.longitude}`}>Xəritədə aç ↗</a>}</article>
    <article className="panel"><h2>Məhsullar</h2>{order.items.map(item => <div className="order-product" key={item.id}><div><strong>{item.productName}</strong><small>{item.productCode} · {item.totalQuantity} × {money(item.unitPrice)}</small><small>Hazır {item.readyQuantity} · Çatdırılıb {item.deliveredQuantity} · Qalıb {item.remainingQuantity}</small></div><b>{money(item.lineTotal)}</b>{item.specialOrderQuantity > 0 && <div className="special-control"><Status value={`${item.specialOrderQuantity} ədəd çatışmayan · ${specialLabels[item.specialOrderStatus]}`} tone="warning" /><select value={item.specialOrderStatus} onChange={event => action('special', `${item.id}|${event.target.value}`)}><option value="Pending">Gözləyir</option><option value="OrderedFromSupplier">Təchizatçıdan sifariş verildi</option><option value="Arrived">Anbara gəldi</option><option value="Completed">Tamamlandı</option><option value="Cancelled">Ləğv edildi</option></select></div>}</div>)}<div className="totals"><span>Məhsullar <b>{money(order.productsTotal)}</b></span><span>Çatdırılma <b>{money(order.deliveryFee)}</b></span><strong>Yekun <b>{money(order.totalAmount)}</b></strong></div></article>
    {active && <article className="panel"><h2>Sifariş axını</h2><div className="button-grid">{order.status === 'PendingAdminApproval' && <button className="primary" onClick={() => action('confirm')}>✓ Təsdiqlə</button>}{!hasActiveShipment && order.status === 'Confirmed' && <button className="primary" onClick={() => action('preparing')}>Hazırlamağa başla</button>}{!hasActiveShipment && order.status === 'Preparing' && order.deliveryMode !== 'PartialAllowed' && order.readyQuantity === order.remainingQuantity && <button className="primary" onClick={() => action('delivering')}>Çatdırılmaya keçir</button>}{!hasActiveShipment && order.status === 'Delivering' && order.readyQuantity === order.remainingQuantity && <button className="primary" onClick={() => action('complete')}>Tamamla</button>}{!hasActiveShipment && order.deliveryMode === 'PartialAllowed' && order.readyQuantity > 0 && <button onClick={() => action('deliverReady')}>Hazır olanı çatdır</button>}{!hasActiveShipment && order.remainingQuantity > 0 && order.status !== 'PendingAdminApproval' && <button onClick={() => action('cancelRemaining')}>Qalanı ləğv et</button>}</div><label>Çatdırılma forması<select disabled={!canChangeDeliveryMode} value={order.deliveryMode} onChange={event => action('mode', event.target.value)}><option value="Undecided">Seçilməyib</option><option value="AllTogether">Hamısı birlikdə</option><option value="PartialAllowed">Hissə-hissə olar</option></select>{!canChangeDeliveryMode && <small>İlk çatdırılmadan sonra bu seçim dəyişdirilə bilməz.</small>}</label>{hasActiveShipment && <p className="hint">Aktiv göndəriş tamamlanmadan və ya ləğv edilmədən birbaşa çatdırılma əməliyyatı aparıla bilməz.</p>}</article>}
    <article className="panel"><h2>Qiymət və qeyd</h2><form className="inline" onSubmit={event => { event.preventDefault(); action('fee', fee); }}><input type="number" min="0" step="0.01" value={fee} onChange={event => setFee(event.target.value)} /><button>Çatdırılmanı saxla</button></form><form className="stack-form" onSubmit={event => { event.preventDefault(); action('note', note); }}><textarea placeholder="Admin qeydi" value={note} onChange={event => setNote(event.target.value)} /><button>Qeydi saxla</button></form></article>
    {active && order.status !== 'PendingAdminApproval' && <article className="panel"><h2>İşçi və çatdırılma</h2>{assignment ? <div className="assigned"><span>👷 {assignment.staffName}</span>{!hasActiveShipment && <button onClick={() => action('release')}>Tapşırığı geri al</button>}</div> : <form className="inline" onSubmit={event => { event.preventDefault(); action('assign', workerId); }}><select required value={workerId} onChange={event => setWorkerId(event.target.value)}><option value="">İşçi seçin</option>{workers.map(worker => <option key={worker.id} value={worker.id}>{fullName(worker)}</option>)}</select><button>Tapşırığı göndər</button></form>}{assignment && order.readyQuantity > 0 && !hasActiveShipment && <button className="primary wide-space" onClick={() => action('shipment')}>Hazır məhsulları işçiyə göndər</button>}{shipments.map(shipment => { const next = nextShipment[shipment.status]; return <div className="shipment" key={shipment.id}><div className="row"><strong>{shipment.staffName}</strong><Status value={shipmentLabels[shipment.status]} /></div>{shipment.items.map(item => <p key={item.orderItemId}>{item.quantity} × {item.productName}</p>)}{next && <button onClick={() => action('shipmentStatus', `${shipment.id}|${next}`)}>{shipmentLabels[next]}</button>}</div>; })}</article>}
    {active && !hasActiveShipment && order.fulfillmentStatus !== 'PartiallyDelivered' && <article className="panel danger-zone"><h2>Sifarişi ləğv et</h2><form className="inline" onSubmit={event => { event.preventDefault(); if (cancelReason.trim()) action('cancel', cancelReason.trim()); }}><input required placeholder="Ləğv səbəbi" value={cancelReason} onChange={event => setCancelReason(event.target.value)} /><button>Ləğv et</button></form></article>}
  </div></section>;
}

function ProductImage({ product }: { product: Product }) { const [failed, setFailed] = useState(false); return product.imageReference && !failed ? <img className="product-image" src={productImageUrl(product.id)} alt={product.name} onError={() => setFailed(true)} /> : <div className="product-image placeholder">🛍️</div>; }

function ProductsScreen({ products, categories, subcategories, demo, back, busy, save, toggle, reload }: { products: Product[]; categories: Category[]; subcategories: Subcategory[]; demo: boolean; back: () => void; busy: boolean; save: (product: Product | null, data: ProductInput, image?: File) => Promise<unknown>; toggle: (product: Product) => Promise<unknown>; reload: () => Promise<void> }) {
  const [editor, setEditor] = useState<Product | 'new' | null>(null);
  const [manageCategories, setManageCategories] = useState(false);
  const [search, setSearch] = useState('');
  const [only, setOnly] = useState<'current' | 'all' | 'active' | 'passive' | 'archived'>('current');
  const [categoryId, setCategoryId] = useState('');
  const [subcategoryId, setSubcategoryId] = useState('');
  const [stockFilter, setStockFilter] = useState<'all' | 'in' | 'out' | 'low'>('all');
  const [selectedIds, setSelectedIds] = useState<string[]>([]);
  const [bulkAction, setBulkAction] = useState<'Activate' | 'Deactivate' | 'Archive' | 'Restore' | 'Move'>('Deactivate');
  const [bulkCategoryId, setBulkCategoryId] = useState('');
  const [bulkSubcategoryId, setBulkSubcategoryId] = useState('');
  const [actionError, setActionError] = useState('');
  const availableSubcategories = subcategories.filter(item => !categoryId || item.categoryId === categoryId);
  const list = products
    .filter(item => `${item.name} ${item.code}`.toLowerCase().includes(search.toLowerCase()))
    .filter(item => !categoryId || item.categoryId === categoryId)
    .filter(item => !subcategoryId || item.subcategoryId === subcategoryId)
    .filter(item => only === 'all' || (only === 'current' ? !item.isArchived : only === 'archived' ? item.isArchived : !item.isArchived && item.isActive === (only === 'active')))
    .filter(item => stockFilter === 'all' || (stockFilter === 'in' ? item.stockQuantity > 0 : stockFilter === 'out' ? item.stockQuantity <= 0 : item.isLowStock));

  async function archive(product: Product) {
    if (demo || !window.confirm(`${product.name} məhsulunu arxivləşdirmək istəyirsiniz?`)) return;
    setActionError('');
    try { await api.archiveProduct(product.id); await reload(); }
    catch (reason) { setActionError(reason instanceof Error ? reason.message : 'Məhsul arxivlənə bilmədi.'); }
  }
  async function restore(product: Product) {
    if (demo) return;
    setActionError('');
    try { await api.restoreProduct(product.id); await reload(); }
    catch (reason) { setActionError(reason instanceof Error ? reason.message : 'Məhsul bərpa edilə bilmədi.'); }
  }
  async function remove(product: Product) {
    if (demo || !window.confirm(`${product.name} məhsulu həmişəlik silinsin? Bu əməliyyat geri qaytarılmır.`)) return;
    setActionError('');
    try {
      await api.deleteProduct(product.id);
      setSelectedIds(values => values.filter(id => id !== product.id));
      await reload();
    } catch (reason) { setActionError(reason instanceof Error ? reason.message : 'Məhsul silinə bilmədi.'); }
  }
  async function applyBulk() {
    if (demo || !selectedIds.length) return;
    if (bulkAction === 'Move' && !bulkCategoryId) return;
    setActionError('');
    try {
      await api.applyBulkProductAction(selectedIds, bulkAction, bulkCategoryId || undefined, bulkSubcategoryId || undefined);
      setSelectedIds([]); await reload();
    } catch (reason) { setActionError(reason instanceof Error ? reason.message : 'Toplu əməliyyat yerinə yetirilə bilmədi.'); }
  }

  return <section className="screen">
    <Heading title="Məhsullar" back={back} action={<button className="heading-action" onClick={() => setEditor('new')}>＋ Əlavə et</button>} />
    <button className="secondary wide-space catalog-manager-button" onClick={() => setManageCategories(true)}>Kateqoriya və ölçüləri idarə et</button>
    <div className="toolbar catalog-toolbar">
      <input className="search" placeholder="Məhsul və ya kod axtar" value={search} onChange={event => setSearch(event.target.value)} />
      <select value={categoryId} onChange={event => { setCategoryId(event.target.value); setSubcategoryId(''); }}><option value="">Bütün kateqoriyalar</option>{categories.filter(item => !item.isArchived).map(item => <option key={item.id} value={item.id}>{item.name}</option>)}</select>
      <select value={subcategoryId} onChange={event => setSubcategoryId(event.target.value)}><option value="">Bütün ölçülər</option>{availableSubcategories.filter(item => !item.isArchived).map(item => <option key={item.id} value={item.id}>{item.name} · {item.categoryName}</option>)}</select>
      <select value={only} onChange={event => setOnly(event.target.value as typeof only)}><option value="current">Cari məhsullar</option><option value="active">Aktiv</option><option value="passive">Passiv</option><option value="archived">Arxiv</option><option value="all">Hamısı, arxiv daxil</option></select>
      <select value={stockFilter} onChange={event => setStockFilter(event.target.value as typeof stockFilter)}><option value="all">Bütün stoklar</option><option value="in">Stokda var</option><option value="out">Stok bitib</option><option value="low">Az qalıb</option></select>
      <button onClick={() => { setSearch(''); setCategoryId(''); setSubcategoryId(''); setOnly('current'); setStockFilter('all'); }}>Filterləri təmizlə</button>
    </div>
    {actionError && <p className="danger-text">{actionError}</p>}
    {!!selectedIds.length && <div className="bulk-bar"><strong>{selectedIds.length} məhsul seçilib</strong><select value={bulkAction} onChange={event => setBulkAction(event.target.value as typeof bulkAction)}><option value="Activate">Aktiv et</option><option value="Deactivate">Passiv et</option><option value="Archive">Arxivlə</option><option value="Restore">Arxivdən çıxar</option><option value="Move">Kateqoriyaya köçür</option></select>{bulkAction === 'Move' && <><select required value={bulkCategoryId} onChange={event => { setBulkCategoryId(event.target.value); setBulkSubcategoryId(''); }}><option value="">Hədəf kateqoriya</option>{categories.filter(item => item.isActive && !item.isArchived).map(item => <option key={item.id} value={item.id}>{item.name}</option>)}</select><select value={bulkSubcategoryId} onChange={event => setBulkSubcategoryId(event.target.value)}><option value="">Ölçüsüz</option>{subcategories.filter(item => item.categoryId === bulkCategoryId && item.isActive && !item.isArchived).map(item => <option key={item.id} value={item.id}>{item.name}</option>)}</select></>}<button className="primary" disabled={bulkAction === 'Move' && !bulkCategoryId} onClick={applyBulk}>Tətbiq et</button><button onClick={() => setSelectedIds([])}>Ləğv et</button></div>}
    <div className="product-grid">{list.map(product => <article className={`product-card ${!product.isActive || product.isArchived ? 'passive' : ''}`} key={product.id}><label className="product-select"><input type="checkbox" checked={selectedIds.includes(product.id)} onChange={event => setSelectedIds(values => event.target.checked ? [...values, product.id] : values.filter(id => id !== product.id))} /> Seç</label><ProductImage product={product} /><div className="product-body"><div className="row"><strong>{product.name}</strong><Status value={product.isArchived ? 'Arxiv' : product.isActive ? 'Aktiv' : 'Passiv'} tone={product.isArchived || !product.isActive ? 'danger' : 'success'} /></div><small>{product.code}</small><p>{product.categoryName || 'Kateqoriyasız'}{product.subcategoryName ? ` · ${product.subcategoryName}` : ''}</p><p>{product.description}</p><div className="row"><b>{money(product.price)}</b><span className={product.isLowStock ? 'danger-text' : ''}>Stok: {product.stockQuantity} · Minimum: {product.minimumStockQuantity}</span></div><div className="card-actions"><button onClick={() => setEditor(product)}>Redaktə et</button>{product.isArchived ? <button onClick={() => restore(product)}>Arxivdən çıxar</button> : <><button className={product.isActive ? 'danger-button' : ''} onClick={() => toggle(product)}>{product.isActive ? 'Passiv et' : 'Aktiv et'}</button><button className="danger-button" onClick={() => archive(product)}>Arxivlə</button></>}<button className="danger-button" onClick={() => remove(product)}>Həmişəlik sil</button></div></div></article>)}</div>
    {!list.length && <Empty text="Məhsul tapılmadı" />}
    {editor && <ProductEditor product={editor === 'new' ? null : editor} categories={categories} subcategories={subcategories} demo={demo} busy={busy} close={() => setEditor(null)} reload={reload} save={async (data, image) => { await save(editor === 'new' ? null : editor, data, image); setEditor(null); }} />}
    {manageCategories && <CategoryManager categories={categories} subcategories={subcategories} demo={demo} close={() => setManageCategories(false)} reload={reload} />}
  </section>;
}

function ProductEditor({ product, categories, subcategories, demo, busy, close, save, reload }: { product: Product | null; categories: Category[]; subcategories: Subcategory[]; demo: boolean; busy: boolean; close: () => void; save: (data: ProductInput, image?: File) => Promise<void>; reload: () => Promise<void> }) {
  const activeCategories = categories.filter(item => item.isActive && !item.isArchived);
  const [name, setName] = useState(product?.name ?? ''); const [code, setCode] = useState(product?.code ?? ''); const [price, setPrice] = useState(String(product?.price ?? '')); const [stock, setStock] = useState('0'); const [description, setDescription] = useState(product?.description ?? ''); const [image, setImage] = useState<File>();
  const [categoryId, setCategoryId] = useState(product?.categoryId ?? activeCategories[0]?.id ?? '');
  const [subcategoryId, setSubcategoryId] = useState(product?.subcategoryId ?? '');
  const [minimumStock, setMinimumStock] = useState(String(product?.minimumStockQuantity ?? 0));
  const availableSubcategories = subcategories.filter(item => item.categoryId === categoryId && item.isActive && !item.isArchived);
  async function createCategoryInline() { if (demo) return; const name = window.prompt('Yeni kateqoriyanın adı')?.trim(); if (!name) return; try { const created = await api.createCategory(name, categories.length); await reload(); setCategoryId(created.id); setSubcategoryId(''); } catch (reason) { window.alert(reason instanceof Error ? reason.message : 'Kateqoriya yaradıla bilmədi.'); } }
  async function createSubcategoryInline() { if (demo || !categoryId) return; const name = window.prompt('Yeni ölçü / alt kateqoriya adı')?.trim(); if (!name) return; try { const created = await api.createSubcategory(categoryId, name, availableSubcategories.length); await reload(); setSubcategoryId(created.id); } catch (reason) { window.alert(reason instanceof Error ? reason.message : 'Alt kateqoriya yaradıla bilmədi.'); } }
  function submit(event: FormEvent) { event.preventDefault(); if (!categoryId) return; save({ name: name.trim(), code: code.trim(), price: Number(price), initialStockQuantity: Number(stock), description: description.trim(), imageReference: product?.imageReference, categoryId, subcategoryId: subcategoryId || undefined, minimumStockQuantity: Number(minimumStock) }, image); }
  return <div className="modal"><form className="sheet" onSubmit={submit}><div className="sheet-title"><div><small>{product ? 'Məhsulu yenilə' : 'Yeni məhsul'}</small><h2>{product?.name ?? 'Kataloqa əlavə et'}</h2></div><button type="button" onClick={close}>×</button></div><label>Ad<input required value={name} onChange={event => setName(event.target.value)} /></label><label>Məhsul kodu<input required disabled={!!product} value={code} onChange={event => setCode(event.target.value)} /></label><div className="two-columns"><label>Kateqoriya<select required value={categoryId} onChange={event => { setCategoryId(event.target.value); setSubcategoryId(''); }}><option value="">Kateqoriya seçin</option>{activeCategories.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}</select><button type="button" className="text-button" onClick={createCategoryInline}>＋ Yeni kateqoriya</button></label><label>Ölçü / alt kateqoriya<select value={subcategoryId} onChange={event => setSubcategoryId(event.target.value)}><option value="">Ölçüsüz</option>{availableSubcategories.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}</select><button type="button" className="text-button" disabled={!categoryId} onClick={createSubcategoryInline}>＋ Yeni ölçü</button></label></div><div className="two-columns"><label>Qiymət<input type="number" min="0.01" step="0.01" required value={price} onChange={event => setPrice(event.target.value)} /></label>{!product && <label>İlkin stok<input type="number" min="0" required value={stock} onChange={event => setStock(event.target.value)} /></label>}<label>Minimum stok<input type="number" min="0" required value={minimumStock} onChange={event => setMinimumStock(event.target.value)} /></label></div><label>Açıqlama<textarea required value={description} onChange={event => setDescription(event.target.value)} /></label><label>Şəkil<input type="file" accept="image/jpeg,image/png,image/webp" onChange={event => setImage(event.target.files?.[0])} /><small>JPG, PNG və ya WEBP · maksimum 5 MB</small></label>{!activeCategories.length && <p className="danger-text">Əvvəlcə aktiv kateqoriya yaradın.</p>}{demo && <p className="hint">Demo rejimində dəyişiklik yalnız bu ekranda saxlanır.</p>}<button className="primary" disabled={busy || !categoryId}>{product ? 'Yadda saxla' : 'Məhsulu əlavə et'}</button></form></div>;
}

function CategoryManager({ categories, subcategories, demo, close, reload }: { categories: Category[]; subcategories: Subcategory[]; demo: boolean; close: () => void; reload: () => Promise<void> }) {
  const [selectedCategoryId, setSelectedCategoryId] = useState(categories.find(item => !item.isArchived)?.id ?? categories[0]?.id ?? '');
  const [showArchived, setShowArchived] = useState(false);
  const [categoryName, setCategoryName] = useState('');
  const [subcategoryName, setSubcategoryName] = useState('');
  const [error, setError] = useState('');
  const visibleCategories = categories.filter(item => showArchived || !item.isArchived);
  const currentCategoryId = visibleCategories.some(item => item.id === selectedCategoryId)
    ? selectedCategoryId
    : visibleCategories[0]?.id ?? '';
  const selectedCategory = visibleCategories.find(item => item.id === currentCategoryId);
  const categorySubcategories = subcategories.filter(item => item.categoryId === currentCategoryId && (showArchived || !item.isArchived));

  async function execute(action: () => Promise<void>) {
    if (demo) { setError('Demo rejimində kateqoriya dəyişiklikləri saxlanmır.'); return; }
    setError('');
    try { await action(); await reload(); }
    catch (reason) { setError(reason instanceof Error ? reason.message : 'Əməliyyat alınmadı.'); }
  }

  async function addCategory(event: FormEvent) {
    event.preventDefault();
    if (!categoryName.trim()) return;
    await execute(async () => { const created = await api.createCategory(categoryName.trim(), categories.length); setSelectedCategoryId(created.id); setCategoryName(''); });
  }

  async function editCategory(category: Category) {
    const name = window.prompt('Kateqoriyanın yeni adı', category.name)?.trim();
    if (!name) return;
    const sortText = window.prompt('Sıralama nömrəsi', String(category.sortOrder));
    if (sortText === null) return;
    await execute(() => api.updateCategory(category.id, name, Number(sortText)));
  }

  async function removeCategory(category: Category) {
    if (category.productCount === 0 && category.subcategoryCount === 0) {
      if (window.confirm(`${category.name} kateqoriyası tam silinsin?`)) await execute(() => api.deleteEmptyCategory(category.id));
      return;
    }
    const choice = window.prompt(
      `“${category.name}” daxilində ${category.productCount} məhsul və ${category.subcategoryCount} alt kateqoriya var.\n\n` +
      '1 — Məhsulları başqa kateqoriyaya köçür və bunu sil\n' +
      '2 — Yalnız kateqoriyanı arxivlə\n' +
      '3 — Məhsulları da arxivlə\n' +
      '4 — Məhsulları, ölçüləri və kateqoriyanı həmişəlik sil\n\nSeçimi yazın: 1, 2, 3 və ya 4');
    if (choice === '2') await execute(() => api.removeCategory(category.id, 'ArchiveCategory'));
    if (choice === '3') await execute(() => api.removeCategory(category.id, 'ArchiveProducts'));
    if (choice === '4' && window.confirm('Kateqoriya, ölçülər və silinə bilən məhsullar həmişəlik silinəcək. Davam edilsin?')) await execute(() => api.removeCategory(category.id, 'DeleteProducts'));
    if (choice === '1') {
      const targets = categories.filter(item => item.id !== category.id && item.isActive && !item.isArchived);
      if (!targets.length) { setError('Köçürmək üçün başqa aktiv kateqoriya yoxdur.'); return; }
      const index = Number(window.prompt(targets.map((item, i) => `${i + 1} — ${item.name}`).join('\n')) ?? 0) - 1;
      if (targets[index]) await execute(() => api.removeCategory(category.id, 'MoveProducts', targets[index].id));
    }
  }

  async function addSubcategory(event: FormEvent) {
    event.preventDefault();
    if (!currentCategoryId || !subcategoryName.trim()) return;
    await execute(async () => { await api.createSubcategory(currentCategoryId, subcategoryName.trim(), categorySubcategories.length); setSubcategoryName(''); });
  }

  async function editSubcategory(subcategory: Subcategory) {
    const name = window.prompt('Alt kateqoriyanın yeni adı', subcategory.name)?.trim();
    if (!name) return;
    const sortText = window.prompt('Sıralama nömrəsi', String(subcategory.sortOrder));
    if (sortText === null) return;
    await execute(() => api.updateSubcategory(subcategory.id, name, Number(sortText)));
  }

  async function removeSubcategory(subcategory: Subcategory) {
    if (subcategory.productCount === 0) {
      if (window.confirm(`${subcategory.name} alt kateqoriyası tam silinsin?`)) await execute(() => api.deleteEmptySubcategory(subcategory.id));
      return;
    }
    const choice = window.prompt(
      `“${subcategory.name}” daxilində ${subcategory.productCount} məhsul var.\n\n` +
      '1 — Məhsulları başqa alt kateqoriyaya köçür və bunu sil\n' +
      '2 — Yalnız alt kateqoriyanı arxivlə\n' +
      '3 — Məhsulları da arxivlə\n' +
      '4 — Məhsulları və alt kateqoriyanı həmişəlik sil\n\nSeçimi yazın: 1, 2, 3 və ya 4');
    if (choice === '2') await execute(() => api.removeSubcategory(subcategory.id, 'ArchiveSubcategory'));
    if (choice === '3') await execute(() => api.removeSubcategory(subcategory.id, 'ArchiveProducts'));
    if (choice === '4' && window.confirm('Alt kateqoriya və silinə bilən məhsullar həmişəlik silinəcək. Davam edilsin?')) await execute(() => api.removeSubcategory(subcategory.id, 'DeleteProducts'));
    if (choice === '1') {
      const targets = subcategories.filter(item => item.id !== subcategory.id && item.isActive && !item.isArchived);
      if (!targets.length) { setError('Köçürmək üçün başqa aktiv alt kateqoriya yoxdur.'); return; }
      const index = Number(window.prompt(targets.map((item, i) => `${i + 1} — ${item.categoryName} / ${item.name}`).join('\n')) ?? 0) - 1;
      if (targets[index]) await execute(() => api.removeSubcategory(subcategory.id, 'MoveProducts', targets[index].id));
    }
  }

  return <div className="modal"><div className="sheet category-manager">
    <div className="sheet-title"><div><small>Kataloq quruluşu</small><h2>Kateqoriya və ölçülər</h2></div><button onClick={close}>×</button></div>
    {error && <p className="danger-text">{error}</p>}
    <form className="inline" onSubmit={addCategory}><input required placeholder="Yeni kateqoriya" value={categoryName} onChange={event => setCategoryName(event.target.value)} /><button>Əlavə et</button></form>
    <button className="secondary" onClick={() => setShowArchived(value => !value)}>{showArchived ? 'Arxivi gizlət' : 'Arxivi göstər'}</button>
    <div className="category-tabs">{visibleCategories.map(category => <button className={currentCategoryId === category.id ? 'active' : ''} key={category.id} onClick={() => setSelectedCategoryId(category.id)}>{category.name}{category.isArchived ? ' · Arxiv' : !category.isActive ? ' · Passiv' : ''}</button>)}</div>
    {selectedCategory && <article className="panel"><div className="row"><div><h3>{selectedCategory.name}</h3><small>{selectedCategory.productCount} məhsul · {selectedCategory.subcategoryCount} alt kateqoriya · sıra {selectedCategory.sortOrder}</small></div><Status value={selectedCategory.isArchived ? 'Arxiv' : selectedCategory.isActive ? 'Aktiv' : 'Passiv'} tone={selectedCategory.isActive && !selectedCategory.isArchived ? 'success' : 'danger'} /></div><div className="card-actions"><button onClick={() => editCategory(selectedCategory)}>Adı və sıranı dəyiş</button>{selectedCategory.isArchived ? <button onClick={() => execute(() => api.restoreCategory(selectedCategory.id))}>Arxivdən çıxar</button> : <button onClick={() => execute(() => api.setCategoryActive(selectedCategory.id, !selectedCategory.isActive))}>{selectedCategory.isActive ? 'Passiv et' : 'Aktiv et'}</button>}<button className="danger-button" onClick={() => removeCategory(selectedCategory)}>Sil / arxivlə</button></div></article>}
    {selectedCategory && !selectedCategory.isArchived && <form className="inline" onSubmit={addSubcategory}><input required placeholder="Yeni ölçü / alt kateqoriya" value={subcategoryName} onChange={event => setSubcategoryName(event.target.value)} /><button>Əlavə et</button></form>}
    <div className="list">{categorySubcategories.map(subcategory => <article className="list-card static" key={subcategory.id}><div className="row"><div><strong>{subcategory.name}</strong><small>{subcategory.productCount} məhsul · sıra {subcategory.sortOrder}</small></div><Status value={subcategory.isArchived ? 'Arxiv' : subcategory.isActive ? 'Aktiv' : 'Passiv'} tone={subcategory.isActive && !subcategory.isArchived ? 'success' : 'danger'} /></div><div className="card-actions"><button onClick={() => editSubcategory(subcategory)}>Dəyiş</button>{subcategory.isArchived ? <button onClick={() => execute(() => api.restoreSubcategory(subcategory.id))}>Arxivdən çıxar</button> : <button onClick={() => execute(() => api.setSubcategoryActive(subcategory.id, !subcategory.isActive))}>{subcategory.isActive ? 'Passiv et' : 'Aktiv et'}</button>}<button className="danger-button" onClick={() => removeSubcategory(subcategory)}>Sil / arxivlə</button></div></article>)}{selectedCategory && !categorySubcategories.length && <Empty text="Alt kateqoriya yoxdur" />}</div>
  </div></div>;
}

function StockScreen({ products, demo, back, change, history }: { products: Product[]; demo: boolean; back: () => void; change: (product: Product, action: StockAction, quantity: number, reason: string) => Promise<unknown>; history: (product: Product) => Promise<StockMovement[]> }) {
  const [selected, setSelected] = useState<Product | null>(null); const [movements, setMovements] = useState<StockMovement[]>([]); const [quantity, setQuantity] = useState('1'); const [reason, setReason] = useState('Yeni mal qəbulu');
  async function open(product: Product) { setSelected(product); setMovements(await history(product)); }
  async function apply(action: StockAction) { if (!selected) return; await change(selected, action, Number(quantity), reason); setSelected(null); }
  return <section className="screen"><Heading title="Stok" back={back} /><div className="list">{products.map(product => <button className="list-card" key={product.id} onClick={() => open(product)}><div className="row"><strong>{product.name}</strong><Status value={product.stockQuantity === 0 ? 'Yoxdur' : product.stockQuantity < 5 ? 'Az qalıb' : 'Normal'} tone={product.stockQuantity < 5 ? 'warning' : 'success'} /></div><div className="row meta"><span>{product.code}</span><b>{product.stockQuantity} ədəd</b></div></button>)}</div>{selected && <div className="modal"><div className="sheet"><div className="sheet-title"><div><small>Stok idarəsi</small><h2>{selected.name}</h2></div><button onClick={() => setSelected(null)}>×</button></div><div className="stock-now">Hazırda <strong>{selected.stockQuantity}</strong> ədəd</div><label>Miqdar<input type="number" min="0" value={quantity} onChange={event => setQuantity(event.target.value)} /></label><label>Səbəb<input value={reason} onChange={event => setReason(event.target.value)} /></label><div className="button-grid"><button onClick={() => apply('add')}>＋ Əlavə et</button><button onClick={() => apply('remove')}>− Çıx</button><button onClick={() => apply('set')}>Dəqiq təyin et</button></div><h3>Son hərəkətlər</h3>{movements.length ? <div className="movement-list">{movements.slice(0, 8).map(item => <div key={item.id}><span>{item.reason}</span><b className={item.quantityChange >= 0 ? 'positive' : 'negative'}>{item.quantityChange > 0 ? '+' : ''}{item.quantityChange}</b><small>{date(item.createdAtUtc)}</small></div>)}</div> : <p className="hint">{demo ? 'Demo tarixçəsi yoxdur.' : 'Stok hərəkəti yoxdur.'}</p>}</div></div>}</section>;
}

function ShortagesScreen({
  items,
  demo,
  back,
  loadOrders,
  exportExcel,
  openOrderNumber,
  notify,
}: {
  items: MissingProduct[];
  demo: boolean;
  back: () => void;
  loadOrders: (productId: string) => Promise<MissingProductOrder[]>;
  exportExcel: () => Promise<void>;
  openOrderNumber: (number: number) => Promise<void>;
  notify: (message: string) => void;
}) {
  const [selected, setSelected] = useState<MissingProduct | null>(null);
  const [affected, setAffected] = useState<MissingProductOrder[]>([]);
  const [busy, setBusy] = useState(false);
  const [exporting, setExporting] = useState(false);
  const [localError, setLocalError] = useState('');
  const copyText = items
    .map(item => `${item.productName} (${item.productCode}) — ${item.missingQuantity} ədəd`)
    .join('\n');

  async function open(item: MissingProduct) {
    setSelected(item);
    setBusy(true);
    setLocalError('');

    try {
      setAffected(await loadOrders(item.productId));
    } catch (reason) {
      setLocalError(reason instanceof Error ? reason.message : 'Detallar açıla bilmədi.');
    } finally {
      setBusy(false);
    }
  }

  async function copy() {
    if (!copyText) return;

    try {
      await navigator.clipboard.writeText(copyText);
      notify('Çatışmazlıq siyahısı kopyalandı.');
    } catch {
      setLocalError('Siyahını kopyalamaq mümkün olmadı.');
    }
  }

  async function exportList() {
    if (!items.length || demo) return;

    setExporting(true);
    setLocalError('');

    try {
      await exportExcel();
      notify('Excel faylı endirildi.');
    } catch (reason) {
      setLocalError(reason instanceof Error
        ? reason.message
        : 'Excel faylını endirmək mümkün olmadı.');
    } finally {
      setExporting(false);
    }
  }

  return <section className="screen">
    <Heading title="Çatışmazlıqlar" back={back} />
    <div className="button-grid">
      <button disabled={!items.length} onClick={copy}>Kopyala</button>
      <button
        className="primary"
        disabled={!items.length || demo || exporting}
        onClick={exportList}
      >
        {exporting ? 'Hazırlanır…' : 'Excel-ə export et'}
      </button>
    </div>
    {demo && <p className="hint">Demo rejimində real çatışmazlıq yoxdur.</p>}
    {localError && <p className="danger-text">{localError}</p>}
    <div className="list">
      {items.map(item => <button className="list-card" key={item.productId} onClick={() => open(item)}>
        <div className="row">
          <strong>{item.productName}</strong>
          <Status value={`${item.missingQuantity} ədəd`} tone="warning" />
        </div>
        <div className="row meta">
          <span>{item.productCode}</span>
          <b>{item.affectedOrderCount} sifariş</b>
        </div>
      </button>)}
      {!items.length && <Empty text="Hazırda çatışmayan məhsul yoxdur" />}
    </div>
    {selected && <div className="modal"><div className="sheet">
      <div className="sheet-title">
        <div><small>Çatışmayan məhsul</small><h2>{selected.productName}</h2></div>
        <button onClick={() => setSelected(null)}>×</button>
      </div>
      <div className="stock-now">Cəmi <strong>{selected.missingQuantity}</strong> ədəd çatmır</div>
      {busy
        ? <span className="spinner" />
        : <div className="list">
          {affected.map(order => <button className="list-card" key={order.orderNumber} onClick={() => openOrderNumber(order.orderNumber)}>
            <div className="row">
              <strong>#{order.orderNumber}</strong>
              <Status value={`${order.missingQuantity} ədəd`} tone="warning" />
            </div>
            <h3>{order.customerName}</h3>
            <small>{specialLabels[order.status]}</small>
          </button>)}
          {!affected.length && <Empty text="Əlaqəli sifariş yoxdur" />}
        </div>}
    </div></div>}
  </section>;
}

function StockPurchasesScreen({ items, products, demo, back, search, create }: { items: StockPurchase[]; products: Product[]; demo: boolean; back: () => void; search: (from: string, to: string) => Promise<unknown>; create: (purchasedAtUtc: string, note: string, items: CreateStockPurchaseItem[]) => Promise<boolean> }) {
  const [from, setFrom] = useState(defaultFrom);
  const [to, setTo] = useState(defaultToday);
  const [editor, setEditor] = useState(false);
  const [selected, setSelected] = useState<StockPurchase | null>(null);
  return <section className="screen"><Heading title="Stok alışları" back={back} action={<button className="heading-action" onClick={() => setEditor(true)}>＋ Alış</button>} /><form className="period-form" onSubmit={event => { event.preventDefault(); void search(from, to); }}><label>Başlanğıc<input type="date" required value={from} max={to} onChange={event => setFrom(event.target.value)} /></label><label>Son tarix<input type="date" required value={to} min={from} onChange={event => setTo(event.target.value)} /></label><button className="primary">Göstər</button></form><div className="list">{items.map(item => <button className="list-card" key={item.id} onClick={() => setSelected(item)}><div className="row"><strong>{date(item.purchasedAtUtc)}</strong><b>{money(item.totalAmount)}</b></div><p>{item.note || 'Qeyd yoxdur'}</p><small>{item.items.length} məhsul mövqeyi</small></button>)}{!items.length && <Empty text="Bu tarix aralığında stok alışı yoxdur" />}</div>{editor && <StockPurchaseEditor products={products} close={() => setEditor(false)} save={async (purchasedAtUtc, note, rows) => { if (await create(purchasedAtUtc, note, rows)) setEditor(false); }} />}{selected && <div className="modal"><div className="sheet"><div className="sheet-title"><div><small>{date(selected.purchasedAtUtc)}</small><h2>Alış detalları</h2></div><button onClick={() => setSelected(null)}>×</button></div>{selected.items.map(item => <div className="purchase-line" key={item.id}><div><strong>{item.productName}</strong><small>{item.productCode} · {item.quantity} × {money(item.unitCost)}</small></div><b>{money(item.lineTotal)}</b></div>)}<div className="totals"><strong>Yekun <b>{money(selected.totalAmount)}</b></strong></div>{selected.note && <p className="hint">Qeyd: {selected.note}</p>}</div></div>}{demo && <p className="hint">Demo rejimində alışlar yalnız bu sessiyada saxlanır.</p>}</section>;
}

function StockPurchaseEditor({ products, close, save }: { products: Product[]; close: () => void; save: (purchasedAtUtc: string, note: string, items: CreateStockPurchaseItem[]) => Promise<void> }) {
  const [purchasedAt, setPurchasedAt] = useState(defaultLocalDateTime);
  const [note, setNote] = useState('');
  const [rows, setRows] = useState<Array<CreateStockPurchaseItem & { key: string }>>([{ key: crypto.randomUUID(), productId: '', quantity: 1, unitCost: 0.01 }]);
  function update(key: string, changes: Partial<CreateStockPurchaseItem>) { setRows(values => values.map(value => value.key === key ? { ...value, ...changes } : value)); }
  return <div className="modal"><form className="sheet" onSubmit={event => { event.preventDefault(); void save(new Date(purchasedAt).toISOString(), note.trim(), rows.map(({ productId, quantity, unitCost }) => ({ productId, quantity, unitCost }))); }}><div className="sheet-title"><div><small>Mal qəbulu</small><h2>Yeni stok alışı</h2></div><button type="button" onClick={close}>×</button></div><label>Alış tarixi<input type="datetime-local" required value={purchasedAt} onChange={event => setPurchasedAt(event.target.value)} /></label><label>Qeyd<textarea value={note} onChange={event => setNote(event.target.value)} placeholder="Faktura və ya təchizatçı qeydi" /></label><h3>Məhsullar</h3>{rows.map((row, index) => <div className="purchase-editor-row" key={row.key}><select required value={row.productId} onChange={event => update(row.key, { productId: event.target.value })}><option value="">Məhsul seçin</option>{products.map(product => <option key={product.id} value={product.id} disabled={rows.some(other => other.key !== row.key && other.productId === product.id)}>{product.name} ({product.code})</option>)}</select><input aria-label="Miqdar" type="number" required min="1" value={row.quantity} onChange={event => update(row.key, { quantity: Number(event.target.value) })} /><input aria-label="Vahid maya dəyəri" type="number" required min="0.01" step="0.01" value={row.unitCost} onChange={event => update(row.key, { unitCost: Number(event.target.value) })} />{rows.length > 1 && <button type="button" onClick={() => setRows(values => values.filter(value => value.key !== row.key))}>×</button>}{index === 0 && <small>Miqdar · vahid maya dəyəri</small>}</div>)}<button type="button" className="text-button" disabled={rows.length >= products.length} onClick={() => setRows(values => [...values, { key: crypto.randomUUID(), productId: '', quantity: 1, unitCost: 0.01 }])}>＋ Məhsul sətri</button><button className="primary">Alışı yadda saxla</button></form></div>;
}

function demoTurnoverReport(fromUtc: string, toUtc: string, companyId: string | undefined, companies: DeliveryCompany[]): InventoryTurnoverReport {
  return { fromUtc, toUtc, deliveryCompanyId: companyId, deliveryCompanyName: companies.find(item => item.id === companyId)?.name, products: [], totalPurchasedQuantity: 0, totalPurchaseAmount: 0, totalSoldQuantity: 0, totalSalesAmount: 0 };
}

function ReportsScreen({ report, companies, back, generate }: { report: InventoryTurnoverReport | null; companies: DeliveryCompany[]; back: () => void; generate: (from: string, to: string, companyId?: string) => Promise<unknown> }) {
  const [from, setFrom] = useState(defaultFrom);
  const [to, setTo] = useState(defaultToday);
  const [companyId, setCompanyId] = useState('');
  return <section className="screen"><Heading title="Satış və dövriyyə" back={back} /><form className="report-form" onSubmit={event => { event.preventDefault(); void generate(from, to, companyId || undefined); }}><div className="two-columns"><label>Başlanğıc<input type="date" required value={from} max={to} onChange={event => setFrom(event.target.value)} /></label><label>Son tarix<input type="date" required value={to} min={from} onChange={event => setTo(event.target.value)} /></label></div><label>Çatdırılma şirkəti<select value={companyId} onChange={event => setCompanyId(event.target.value)}><option value="">Bütün şirkətlər</option>{companies.map(company => <option key={company.id} value={company.id}>{company.name}{company.isActive ? '' : ' (passiv)'}</option>)}</select></label><button className="primary">Hesabatı hazırla</button></form>{report ? <><div className="report-summary"><div><span>Satış miqdarı</span><strong>{report.totalSoldQuantity}</strong></div><div><span>Satış məbləği</span><strong>{money(report.totalSalesAmount)}</strong></div><div><span>Alış miqdarı</span><strong>{report.totalPurchasedQuantity}</strong></div><div><span>Alış məbləği</span><strong>{money(report.totalPurchaseAmount)}</strong></div></div>{report.deliveryCompanyName && <p className="hint">Filtr: {report.deliveryCompanyName}</p>}<div className="list">{report.products.map(product => <article className="list-card static" key={product.productId}><div className="row"><strong>{product.productName}</strong><small>{product.productCode}</small></div><div className="turnover-grid"><span>Satılıb <b>{product.soldQuantity}</b></span><span>Satış <b>{money(product.salesAmount)}</b></span><span>Alınıb <b>{product.purchasedQuantity}</b></span><span>Alış <b>{money(product.purchaseAmount)}</b></span></div></article>)}{!report.products.length && <Empty text="Seçilən dövr üçün dövriyyə yoxdur" />}</div></> : <Empty text="Tarix və şirkət filtrini seçib hesabat yaradın" />}</section>;
}

function DeliveryCompaniesScreen({ items, back, add, rename, toggle }: { items: DeliveryCompany[]; back: () => void; add: (name: string) => Promise<boolean>; rename: (company: DeliveryCompany, name: string) => Promise<boolean>; toggle: (company: DeliveryCompany) => Promise<unknown> }) {
  const [newName, setNewName] = useState('');
  const [editing, setEditing] = useState<DeliveryCompany | null>(null);
  const [editName, setEditName] = useState('');
  return <section className="screen"><Heading title="Çatdırılma şirkətləri" back={back} /><form className="inline" onSubmit={event => { event.preventDefault(); const name = newName.trim(); if (name) void add(name).then(ok => { if (ok) setNewName(''); }); }}><input required maxLength={100} placeholder="Yeni şirkətin adı" value={newName} onChange={event => setNewName(event.target.value)} /><button className="primary">Əlavə et</button></form><div className="list">{items.map(company => <article className="list-card static" key={company.id}><div className="row"><h3>🚚 {company.name}</h3><Status value={company.isActive ? 'Aktiv' : 'Passiv'} tone={company.isActive ? 'success' : 'danger'} /></div><small>Əlavə edilib: {date(company.createdAtUtc)}</small><div className="card-actions"><button onClick={() => { setEditing(company); setEditName(company.name); }}>Adı dəyiş</button><button className={company.isActive ? 'danger-button' : ''} onClick={() => toggle(company)}>{company.isActive ? 'Passiv et' : 'Aktiv et'}</button></div></article>)}{!items.length && <Empty text="Çatdırılma şirkəti əlavə edilməyib" />}</div>{editing && <div className="modal"><form className="sheet" onSubmit={event => { event.preventDefault(); const name = editName.trim(); if (name) void rename(editing, name).then(ok => { if (ok) setEditing(null); }); }}><div className="sheet-title"><h2>Şirkətin adını dəyiş</h2><button type="button" onClick={() => setEditing(null)}>×</button></div><label>Ad<input required maxLength={100} value={editName} onChange={event => setEditName(event.target.value)} /></label><button className="primary">Yadda saxla</button></form></div>}</section>;
}

function StaffScreen({ staff, back, add, toggle }: { staff: Staff[]; back: () => void; add: (data: { telegramUserId: number; firstName: string; lastName?: string; username?: string }) => Promise<unknown>; toggle: (worker: Staff) => Promise<unknown> }) {
  const [form, setForm] = useState(false); const workers = staff.filter(item => item.role === 'Worker');
  return <section className="screen"><Heading title="İşçilər" back={back} action={<button className="heading-action" onClick={() => setForm(true)}>＋ İşçi</button>} /><div className="summary-row"><div><strong>{workers.filter(item => item.isActive).length}</strong><span>Aktiv</span></div><div><strong>{workers.filter(item => !item.isActive).length}</strong><span>Passiv</span></div></div><div className="list">{workers.map(worker => <article className="list-card static" key={worker.id}><div className="row"><h3>👷 {fullName(worker)}</h3><Status value={worker.isActive ? 'Aktiv' : 'Passiv'} tone={worker.isActive ? 'success' : 'danger'} /></div><p>{worker.username ? `@${worker.username}` : `Telegram ID: ${worker.telegramUserId}`}</p><button className="text-button" onClick={() => toggle(worker)}>{worker.isActive ? 'Passiv et' : 'Yenidən aktiv et'}</button></article>)}{!workers.length && <Empty text="İşçi əlavə edilməyib" />}</div>{form && <WorkerForm close={() => setForm(false)} save={async data => { await add(data); setForm(false); }} />}</section>;
}

function WorkerForm({ close, save }: { close: () => void; save: (data: { telegramUserId: number; firstName: string; lastName?: string; username?: string }) => Promise<void> }) {
  const [id, setId] = useState(''); const [first, setFirst] = useState(''); const [last, setLast] = useState(''); const [username, setUsername] = useState('');
  return <div className="modal"><form className="sheet" onSubmit={event => { event.preventDefault(); save({ telegramUserId: Number(id), firstName: first.trim(), lastName: last.trim() || undefined, username: username.replace('@', '').trim() || undefined }); }}><div className="sheet-title"><h2>Yeni işçi</h2><button type="button" onClick={close}>×</button></div><label>Telegram ID<input type="number" required value={id} onChange={event => setId(event.target.value)} /></label><label>Ad<input required value={first} onChange={event => setFirst(event.target.value)} /></label><label>Soyad<input value={last} onChange={event => setLast(event.target.value)} /></label><label>Username<input value={username} onChange={event => setUsername(event.target.value)} /></label><button className="primary">Əlavə et</button></form></div>;
}

function InvitesScreen({ demo, back, create }: { demo: boolean; back: () => void; create: () => Promise<string> }) {
  const [url, setUrl] = useState('');
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState('');

  async function createInvite() {
    setBusy(true);
    setMessage('');
    try {
      setUrl(await create());
      setMessage('Yeni birdəfəlik link yaradıldı.');
    } catch (reason) {
      setMessage(reason instanceof Error ? reason.message : 'Link yaratmaq mümkün olmadı.');
    } finally {
      setBusy(false);
    }
  }

  async function copyInvite() {
    if (!url) return;
    try {
      await navigator.clipboard.writeText(url);
      setMessage('Link kopyalandı.');
    } catch {
      setMessage('Linki seçib əl ilə kopyalayın.');
    }
  }

  return <section className="screen">
    <Heading title="Dəvət linkləri" back={back} />
    <article className="panel invite-panel">
      <span className="invite-icon">🔗</span>
      <h2>Yeni müştəri dəvəti</h2>
      <p>Müştəri linkə daxil olub START düyməsinə basacaq. Link yalnız ilk Telegram hesabı üçün işləyəcək.</p>
      <button className="primary wide-space" disabled={busy} onClick={createInvite}>
        {busy ? 'Yaradılır…' : 'Yeni link yarat'}
      </button>
      {url && <div className="invite-result">
        <input readOnly value={url} onFocus={event => event.currentTarget.select()} />
        <div className="invite-actions">
          <button onClick={copyInvite}>📋 Kopyala</button>
          <a href={`https://t.me/share/url?url=${encodeURIComponent(url)}`} target="_blank" rel="noreferrer">Telegram-da paylaş</a>
        </div>
      </div>}
      {message && <small>{message}</small>}
      {demo && <p className="hint">Demo rejimində yaradılan link database-ə yazılmır.</p>}
    </article>
  </section>;
}

function CustomersScreen({ customers, orders, demo, back, loadOrders, block }: { customers: Customer[]; orders: Order[]; demo: boolean; back: () => void; loadOrders: (id: string) => Promise<Order[]>; block: (customer: Customer) => Promise<boolean> }) {
  const [selected, setSelected] = useState<Customer | null>(null);
  const [customerOrders, setCustomerOrders] = useState<Order[]>([]);
  const [search, setSearch] = useState('');
  const list = customers.filter(item =>
    !item.isBlocked &&
    `${fullName(item)} ${item.phoneNumber} ${item.username}`.toLowerCase().includes(search.toLowerCase()));

  async function open(customer: Customer) {
    setSelected(customer);
    setCustomerOrders(await loadOrders(customer.id));
  }

  async function confirmBlock(customer: Customer) {
    const confirmed = window.confirm(
      `${fullName(customer)} üçün bot girişini bağlamaq istəyirsiniz?\n\n` +
      'Sifariş tarixçəsi silinməyəcək. Müştəri yalnız yeni dəvət linki ilə yenidən daxil ola biləcək.');

    if (!confirmed)
      return;

    if (await block(customer))
      setSelected(null);
  }

  return <section className="screen">
    <Heading title="Müştərilər" back={back} />
    <input className="search" placeholder="Ad, telefon və username" value={search} onChange={event => setSearch(event.target.value)} />
    <div className="list top-gap">
      {list.map(customer => <article className="list-card customer-card" key={customer.id}>
        <button className="customer-open" onClick={() => open(customer)}>
          <div className="row">
            <h3>👤 {fullName(customer)}</h3>
            <b>{orders.filter(item => item.customerId === customer.id).length || ''}</b>
          </div>
          <p>📞 {customer.phoneNumber || 'Nömrə yoxdur'}</p>
          <p>📍 {customer.deliveryAddress || 'Ünvan yoxdur'}</p>
        </button>
        <button className="customer-block" aria-label={`${fullName(customer)} üçün bot girişini bağla`} title="Bot girişini bağla" onClick={() => confirmBlock(customer)}>×</button>
      </article>)}
      {!list.length && <Empty text="Axtarışa uyğun müştəri yoxdur" />}
    </div>
    {selected && <div className="modal"><div className="sheet"><div className="sheet-title"><div><small>Müştəri profili</small><h2>{fullName(selected)}</h2></div><button onClick={() => setSelected(null)}>×</button></div><a href={`tel:${selected.phoneNumber}`}>📞 {selected.phoneNumber || 'Nömrə yoxdur'}</a><p>📍 {selected.deliveryAddress || 'Ünvan yoxdur'}</p>{selected.username && <p>Telegram: @{selected.username}</p>}<h3>Sifarişlər ({customerOrders.length})</h3><div className="movement-list">{customerOrders.map(order => <div key={order.id}><span>#{order.orderNumber} · {orderLabels[order.status]}</span><b>{money(order.totalAmount)}</b><small>{date(order.createdAtUtc)}</small></div>)}</div>{demo && <p className="hint">Demo müştəri məlumatlarıdır.</p>}</div></div>}
  </section>;
}

function WorkerPanel({ assignments, shipments, loading, demo, error, onRefresh, onChange }: { assignments: Assignment[]; shipments: Shipment[]; loading: boolean; demo: boolean; error: string; onRefresh: () => void; onChange: (shipment: Shipment, status: ShipmentStatus) => void }) {
  const [tab, setTab] = useState<'active' | 'history'>('active');
  const active = useMemo(() => shipments.filter(item => !['Delivered', 'Cancelled'].includes(item.status)), [shipments]);
  const history = useMemo(() => shipments.filter(item => ['Delivered', 'Cancelled'].includes(item.status)), [shipments]);
  const pending = useMemo(() => assignments.filter(assignment => !shipments.some(shipment => shipment.orderNumber === assignment.orderNumber && shipment.status !== 'Cancelled')), [assignments, shipments]);
  const list = tab === 'active' ? active : history;
  const activeCount = active.length + pending.length;
  return <main className="app-shell"><Header demo={demo} subtitle="İşçi paneli" />{error && <div className="toast error">{error}</div>}<section className="screen"><div className="hero"><span>Tapşırıqlarım</span><h1>{activeCount} aktiv tapşırıq</h1><p>Yalnız sizə təyin edilmiş sifarişləri görürsünüz.</p><button className="refresh-button" disabled={loading} onClick={onRefresh}>↻ Yenilə</button></div><div className="tabs"><button className={tab === 'active' ? 'active' : ''} onClick={() => setTab('active')}>Aktiv ({activeCount})</button><button className={tab === 'history' ? 'active' : ''} onClick={() => setTab('history')}>Tarixçə ({history.length})</button></div><div className="list top-gap">{tab === 'active' && pending.map(item => <article className="worker-card pending-task" key={item.id}><div className="row"><h2>#{item.orderNumber}</h2><Status value="Təyin edilib" tone="info" /></div><p>Admin bu sifarişi sizə təyin edib.</p><small>Məhsullar hazır olduqda çatdırılma məlumatları burada görünəcək.</small></article>)}{list.map(shipment => { const next = nextShipment[shipment.status]; return <article className="worker-card" key={shipment.id}><div className="row"><h2>#{shipment.orderNumber}</h2><Status value={shipmentLabels[shipment.status]} tone={shipment.status === 'Delivered' ? 'success' : 'info'} /></div><div className="worker-links"><a href={`tel:${shipment.phoneNumber}`}>📞 Zəng et</a><a target="_blank" rel="noreferrer" href={shipment.latitude && shipment.longitude ? `https://maps.google.com/?q=${shipment.latitude},${shipment.longitude}` : `https://maps.google.com/?q=${encodeURIComponent(shipment.deliveryAddress)}`}>📍 Xəritə</a></div><p>{shipment.deliveryAddress}</p><div className="products-box">{shipment.items.map(item => <div key={item.orderItemId}><strong>{item.quantity} ×</strong> {item.productName}</div>)}</div>{next && <button className="primary" disabled={loading} onClick={() => onChange(shipment, next)}>{shipmentLabels[next]}</button>}</article>; })}{!list.length && !(tab === 'active' && pending.length) && <Empty text={tab === 'active' ? 'Aktiv tapşırıq yoxdur' : 'Tarixçə boşdur'} />}</div></section>{loading && <div className="busy"><span className="spinner" /></div>}</main>;
}

function BottomNav({ section, open }: { section: Section; open: (value: Section) => void }) { return <nav className="bottom-nav"><button className={section === 'home' ? 'active' : ''} onClick={() => open('home')}><span>⌂</span>Ana səhifə</button><button className={section === 'orders' ? 'active' : ''} onClick={() => open('orders')}><span>▣</span>Sifarişlər</button><button className={section === 'products' ? 'active' : ''} onClick={() => open('products')}><span>◇</span>Məhsullar</button><button className={section === 'staff' ? 'active' : ''} onClick={() => open('staff')}><span>♙</span>İşçilər</button></nav>; }

import { useEffect, useMemo, useState, type FormEvent } from 'react';
import { api, productImageUrl } from './client';
import CustomerApp from './CustomerApp';
import {
  deleteCustomerIdentity,
  getCustomerIdentity,
  isNetworkFailure,
  saveCustomerIdentity,
  telegramUserIdFromInitData,
} from './offlineDb';
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
  PendingAdminApproval: 'Новые', Confirmed: 'Подтверждён', Preparing: 'Готовится', Delivering: 'Доставляется', Completed: 'Завершён', Cancelled: 'Отменён',
};
const shipmentLabels: Record<ShipmentStatus, string> = {
  Assigned: 'Назначено', Preparing: 'Готовится', Ready: 'Готово', OutForDelivery: 'Доставляется', Delivered: 'Доставлено', Cancelled: 'Отменён',
};
const specialLabels: Record<SpecialOrderStatus, string> = {
  NotRequired: 'Со склада', Pending: 'Ожидает', OrderedFromSupplier: 'Заказано у поставщика', Arrived: 'Поступило на склад', Completed: 'Завершено', Cancelled: 'Отменено',
};
const nextShipment: Partial<Record<ShipmentStatus, ShipmentStatus>> = {
  Assigned: 'Preparing', Preparing: 'Ready', Ready: 'OutForDelivery', OutForDelivery: 'Delivered',
};

function money(value?: number) { return `${(value ?? 0).toFixed(2)} AZN`; }
function date(value?: string) { return value ? new Intl.DateTimeFormat('ru-RU', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value)) : '—'; }
function inputDate(value: Date) { return value.toISOString().slice(0, 10); }
function periodBounds(from: string, to: string) {
  const start = new Date(`${from}T00:00:00`);
  const end = new Date(`${to}T00:00:00`);
  end.setDate(end.getDate() + 1);
  return [start.toISOString(), end.toISOString()] as const;
}
function fullName(value?: Pick<Customer | Staff, 'firstName' | 'lastName'>) { return value ? [value.firstName, value.lastName].filter(Boolean).join(' ') : 'Неизвестный клиент'; }
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
        : 'У вас нет доступа.';

      const telegramUserId = telegramUserIdFromInitData();
      if (telegramUserId !== null) {
        void deleteCustomerIdentity(telegramUserId).catch(() => undefined);
      }

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
          'У вас нет доступа. Откройте Mini App кнопкой в Telegram-боте.',
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
        await saveCustomerIdentity(user).catch(() => undefined);
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
      .catch(async reason => {
        const telegramUserId = telegramUserIdFromInitData();
        const cachedIdentity = isNetworkFailure(reason) && telegramUserId !== null
          ? await getCustomerIdentity(telegramUserId).catch(() => null)
          : null;

        if (cachedIdentity) {
          setIdentity(cachedIdentity);
          setError('');
          return;
        }

        setError(reason instanceof Error ? reason.message : 'Не удалось войти.');
      })
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
        if (!disposed) setError(reason instanceof Error ? reason.message : 'Не удалось обновить задания.');
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
    catch (reason) { setError(reason instanceof Error ? reason.message : 'Не удалось выполнить операцию.'); return false; }
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
    }, 'Данные обновлены.');
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
    }, 'Заказ открыт.');
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
        if (action === 'cancel') updateDemoOrder(number, { status: 'Cancelled', cancellationReason: value || 'Отменено администратором', cancelledAtUtc: now });
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
      if (action === 'cancel') await api.cancelOrder(number, value || 'Отменено администратором');
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
    }, 'Заказ обновлён.');
  }

  if (loading && !demo && !identity) return <Startup />;
  if (error && !demo && !identity) return <Startup error={error} />;
  if (isCustomer) return <CustomerApp identity={identity} demo={demo} />;
  if (isWorker) return <WorkerPanel assignments={workerAssignments} shipments={workerShipments} loading={loading} demo={demo} error={error} onRefresh={() => run(async () => {
    if (demo) return;
    const [assigned, deliveries] = await Promise.all([api.getWorkerAssignments(), api.getWorkerShipments()]);
    setWorkerAssignments(assigned);
    setWorkerShipments(deliveries);
  }, 'Задания обновлены.')} onChange={(shipment, status) => run(async () => {
    if (demo) setWorkerShipments(items => items.map(item => item.id === shipment.id ? { ...item, status } : item));
    else { const updated = await api.changeShipmentStatus(shipment.id, status); setWorkerShipments(items => items.map(item => item.id === updated.id ? updated : item)); }
  }, 'Статус задания обновлён.')} />;

  return <main className="app-shell">
    <Header demo={demo} subtitle="Панель администратора" />
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
    }, product ? 'Товар обновлён.' : 'Товар добавлен.')} toggle={product => run(async () => {
      if (!demo) await api.setProductActive(product.id, !product.isActive);
      setProducts(items => items.map(item => item.id === product.id ? { ...item, isActive: !item.isActive } : item));
    }, product.isActive ? 'Товар скрыт из каталога.' : 'Товар активирован.')} />}
    {section === 'stock' && <StockScreen products={products} demo={demo} back={() => openSection('home')} change={(product, action, quantity, reason) => run(async () => {
      if (!demo) await api.changeStock(product.id, action, quantity, reason);
      setProducts(items => items.map(item => item.id === product.id ? { ...item, stockQuantity: action === 'set' ? quantity : item.stockQuantity + (action === 'add' ? quantity : -quantity) } : item));
    }, 'Остаток обновлён.')} history={async product => demo ? [] : api.getStockHistory(product.id)} />}
    {section === 'shortages' && <ShortagesScreen items={shortages} demo={demo} back={() => openSection('home')} loadOrders={productId => demo ? Promise.resolve([]) : api.getShortageOrders(productId)} exportExcel={() => demo ? Promise.resolve() : api.exportShortages()} openOrderNumber={async number => {
      const order = demo ? orders.find(item => item.orderNumber === number) : await api.getOrder(number);
      if (order) { setSection('orders'); await openOrder(order); }
    }} notify={message => setNotice(message)} />}
    {section === 'purchases' && <StockPurchasesScreen items={stockPurchases} products={products} demo={demo} back={() => openSection('home')} search={(from, to) => run(async () => {
      const [fromUtc, toUtc] = periodBounds(from, to);
      setStockPurchases(demo ? stockPurchases : await api.getStockPurchases(fromUtc, toUtc));
    }, 'История закупок обновлена.')} create={async (purchasedAtUtc, note, items) => run(async () => {
      const created = demo ? { id: crypto.randomUUID(), purchasedAtUtc, note, items: items.map((item, index) => { const product = products.find(value => value.id === item.productId)!; return { id: `${index}`, ...item, productName: product.name, productCode: product.code, lineTotal: item.quantity * item.unitCost }; }), totalAmount: items.reduce((sum, item) => sum + item.quantity * item.unitCost, 0), createdAtUtc: now } : await api.createStockPurchase(purchasedAtUtc, note, items);
      setStockPurchases(values => [created, ...values]);
      if (!demo) setProducts(await api.getAllProducts());
    }, 'Закупка зарегистрирована.')} />}
    {section === 'reports' && <ReportsScreen report={turnoverReport} companies={deliveryCompanies} back={() => openSection('home')} generate={(from, to, companyId) => run(async () => {
      const [fromUtc, toUtc] = periodBounds(from, to);
      setTurnoverReport(demo ? demoTurnoverReport(fromUtc, toUtc, companyId, deliveryCompanies) : await api.getTurnoverReport(fromUtc, toUtc, companyId));
    }, 'Отчёт сформирован.')} />}
    {section === 'companies' && <DeliveryCompaniesScreen items={deliveryCompanies} back={() => openSection('home')} add={name => run(async () => {
      const created = demo ? { id: crypto.randomUUID(), name, isActive: true, createdAtUtc: now } : await api.createDeliveryCompany(name);
      setDeliveryCompanies(values => [...values, created]);
    }, 'Служба доставки добавлена.')} rename={(company, name) => run(async () => {
      const updated = demo ? { ...company, name, updatedAtUtc: now } : await api.renameDeliveryCompany(company.id, name);
      setDeliveryCompanies(values => values.map(value => value.id === company.id ? updated : value));
    }, 'Название службы обновлено.')} toggle={company => run(async () => {
      if (!demo) await api.setDeliveryCompanyActive(company.id, !company.isActive);
      setDeliveryCompanies(values => values.map(value => value.id === company.id ? { ...value, isActive: !value.isActive } : value));
    }, company.isActive ? 'Служба деактивирована.' : 'Служба активирована.')} />}
    {section === 'staff' && <StaffScreen staff={staff} back={() => openSection('home')} add={data => run(async () => {
      const created = demo ? { id: crypto.randomUUID(), role: 'Worker' as const, isActive: true, ...data } : await api.createWorker(data);
      setStaff(items => [...items, created]);
    }, 'Сотрудник добавлен.')} toggle={worker => run(async () => {
      if (!demo) await api.setWorkerActive(worker.id, !worker.isActive);
      setStaff(items => items.map(item => item.id === worker.id ? { ...item, isActive: !item.isActive } : item));
    }, 'Статус сотрудника обновлён.')} />}
    {section === 'customers' && <CustomersScreen customers={customers} orders={orders} demo={demo} back={() => openSection('home')} loadOrders={id => demo ? Promise.resolve(orders.filter(item => item.customerId === id)) : api.getCustomerOrders(id)} block={customer => run(async () => {
      if (!demo) await api.blockCustomer(customer.id);
      setCustomers(items => items.map(item => item.id === customer.id ? { ...item, isBlocked: true, blockedAtUtc: now } : item));
    }, 'Доступ клиента к боту закрыт. Он сможет войти снова по новой ссылке-приглашению.')} />}
    {section === 'invites' && <InvitesScreen demo={demo} back={() => openSection('home')} create={async () => demo ? `https://t.me/StockMateAzBot?start=${crypto.randomUUID().replaceAll('-', '')}` : (await api.createCustomerInvite()).url} />}
    <BottomNav section={section} open={openSection} />
    {loading && <div className="busy"><span className="spinner" /></div>}
  </main>;
}

function Startup({ error }: { error?: string }) { return <div className="startup"><span className={error ? 'startup-icon' : 'spinner'}>{error ? '⚠️' : ''}</span><strong>{error ? 'Не удалось войти' : 'StockMate открывается…'}</strong>{error && <p>{error}</p>}</div>; }
function Header({ demo, subtitle }: { demo: boolean; subtitle: string }) { return <header className="app-header"><div className="brand">S</div><div className="brand-copy"><strong>StockMate</strong><span>{subtitle}</span></div><span className="connection">{demo ? 'Демо' : 'Telegram'}</span></header>; }
function Messages({ error, notice, clear }: { error: string; notice: string; clear: () => void }) { return error || notice ? <div className={`toast ${error ? 'error' : ''}`} role="status"><span>{error || notice}</span><button onClick={clear}>×</button></div> : null; }
function Heading({ title, back, action }: { title: string; back: () => void; action?: React.ReactNode }) { return <div className="page-heading"><button className="back" onClick={back}>‹</button><div><span>Управление</span><h1>{title}</h1></div>{action}</div>; }
function Empty({ text }: { text: string }) { return <div className="empty"><span>📭</span><strong>{text}</strong></div>; }

function Home({ orders, products, staff, open }: { orders: Order[]; products: Product[]; staff: Staff[]; open: (value: Section) => void }) {
  const cards: Array<[Section, string, string, string, number]> = [
    ['orders', '📦', 'Заказы', 'Весь процесс заказов', orders.filter(item => !['Completed', 'Cancelled'].includes(item.status)).length],
    ['products', '🛍️', 'Товары', 'Каталог и цены', products.length],
    ['stock', '📊', 'Склад', 'Склад и движения', products.filter(item => item.stockQuantity < 5).length],
    ['shortages', '⚠️', 'Недостающие товары', 'Недостающие товары по заказам', 0],
    ['purchases', '🧾', 'Закупки', 'Приём товара и история закупок', 0],
    ['reports', '📈', 'Отчёты', 'Анализ продаж и оборота', 0],
    ['companies', '🚚', 'Службы доставки', 'Управление службами', 0],
    ['staff', '👷', 'Сотрудники', 'Команда и статусы', staff.filter(item => item.role === 'Worker' && item.isActive).length],
    ['customers', '👥', 'Клиенты', 'Профиль и заказы', 0],
    ['invites', '🔗', 'Ссылки-приглашения', 'Одноразовая ссылка для нового клиента', 0],
    ['history', '📜', 'История', 'Завершённые и отменённые', orders.filter(item => ['Completed', 'Cancelled'].includes(item.status)).length],
  ];
  return <section className="screen"><div className="hero"><span>Центр управления</span><h1>Чем управляем сегодня?</h1><p>Заказы, склад, товары и сотрудники в одном месте.</p></div><div className="dashboard-grid">{cards.map(([id, icon, title, subtitle, count]) => <button key={id} className="dashboard-card" onClick={() => open(id)}><span className="card-icon">{icon}</span><span><strong>{title}</strong><small>{subtitle}</small></span>{count > 0 && <b>{count}</b>}<em>›</em></button>)}</div></section>;
}

function OrdersScreen({ orders, customers, history, back, open }: { orders: Order[]; customers: Customer[]; history: boolean; back: () => void; open: (order: Order) => void }) {
  const [filter, setFilter] = useState<OrderFilter>(history ? 'completed' : 'new');
  const [search, setSearch] = useState('');
  const filters: OrderFilter[] = history ? ['completed', 'cancelled'] : ['new', 'active', 'partial', 'special'];
  const labels: Record<OrderFilter, string> = { new: 'Новые', active: 'Активные', partial: 'Неполные', special: 'С недостачей', completed: 'Завершённые', cancelled: 'Отменённые' };
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
  return <section className="screen"><Heading title={history ? 'История' : 'Заказы'} back={back} /><div className="filters">{filters.map(value => <button key={value} className={filter === value ? 'active' : ''} onClick={() => setFilter(value)}><strong>{orders.filter(item => filterOrder(item, value)).length}</strong><span>{labels[value]}</span></button>)}</div><input className="search" placeholder="Поиск по №, имени, телефону или адресу" value={search} onChange={event => setSearch(event.target.value)} /><div className="list">{list.length ? list.map(order => { const customer = customers.find(item => item.id === order.customerId); return <button className="list-card" key={order.id} onClick={() => open(order)}><div className="row"><strong>#{order.orderNumber}</strong><Status value={orderLabels[order.status]} tone={order.status === 'Cancelled' ? 'danger' : order.status === 'Completed' ? 'success' : 'warning'} /></div><h3>{fullName(customer)}</h3><p>📍 {order.deliveryAddress}</p><div className="row meta"><span>{date(order.createdAtUtc)}</span><b>{money(order.totalAmount)}</b></div></button>; }) : <Empty text="Заказы по вашему запросу не найдены" />}</div></section>;
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
    ? `${order.items.reduce((sum, item) => sum + item.deliveredQuantity, 0)} доставлено · ${order.remainingQuantity} осталось`
    : `${order.readyQuantity}/${order.remainingQuantity} готово`;
  return <section className="screen"><Heading title={`Заказ №${order.orderNumber}`} back={back} /><div className="detail-stack">
    <article className="panel"><div className="row"><div><small>Статус</small><h2>{orderLabels[order.status]}</h2></div><Status value={progressText} tone={order.fulfillmentStatus === 'PartiallyDelivered' ? 'warning' : 'info'} /></div><p>🕒 {date(order.createdAtUtc)}</p>{order.cancellationReason && <p className="danger-text">Причина: {order.cancellationReason}</p>}</article>
    <article className="panel"><h2>Клиент и доставка</h2><h3>{fullName(customer)}</h3><a href={`tel:${order.phoneNumber}`}>📞 {order.phoneNumber}</a><p>📍 {order.deliveryAddress}</p>{order.deliveryCompanyName && <p>🚚 {order.deliveryCompanyName}</p>}{order.latitude && order.longitude && <a target="_blank" rel="noreferrer" href={`https://maps.google.com/?q=${order.latitude},${order.longitude}`}>Открыть на карте ↗</a>}</article>
    <article className="panel"><h2>Товары</h2>{order.items.map(item => <div className="order-product" key={item.id}><div><strong>{item.productName}</strong><small>{item.productCode} · {item.totalQuantity} × {money(item.unitPrice)}</small><small>Готово {item.readyQuantity} · Доставлено {item.deliveredQuantity} · Осталось {item.remainingQuantity}</small></div><b>{money(item.lineTotal)}</b>{item.specialOrderQuantity > 0 && <div className="special-control"><Status value={`Не хватает: ${item.specialOrderQuantity} шт. · ${specialLabels[item.specialOrderStatus]}`} tone="warning" /><select value={item.specialOrderStatus} onChange={event => action('special', `${item.id}|${event.target.value}`)}><option value="Pending">Ожидает</option><option value="OrderedFromSupplier">Заказано у поставщика</option><option value="Arrived">Поступило на склад</option><option value="Completed">Завершено</option><option value="Cancelled">Отменено</option></select></div>}</div>)}<div className="totals"><span>Товары <b>{money(order.productsTotal)}</b></span><span>Доставка <b>{money(order.deliveryFee)}</b></span><strong>Итого <b>{money(order.totalAmount)}</b></strong></div></article>
    {active && <article className="panel"><h2>Обработка заказа</h2><div className="button-grid">{order.status === 'PendingAdminApproval' && <button className="primary" onClick={() => action('confirm')}>✓ Подтвердить</button>}{!hasActiveShipment && order.status === 'Confirmed' && <button className="primary" onClick={() => action('preparing')}>Начать подготовку</button>}{!hasActiveShipment && order.status === 'Preparing' && order.deliveryMode !== 'PartialAllowed' && order.readyQuantity === order.remainingQuantity && <button className="primary" onClick={() => action('delivering')}>Передать в доставку</button>}{!hasActiveShipment && order.status === 'Delivering' && order.readyQuantity === order.remainingQuantity && <button className="primary" onClick={() => action('complete')}>Завершить</button>}{!hasActiveShipment && order.deliveryMode === 'PartialAllowed' && order.readyQuantity > 0 && <button onClick={() => action('deliverReady')}>Доставить готовое</button>}{!hasActiveShipment && order.remainingQuantity > 0 && order.status !== 'PendingAdminApproval' && <button onClick={() => action('cancelRemaining')}>Отменить остаток</button>}</div><label>Формат доставки<select disabled={!canChangeDeliveryMode} value={order.deliveryMode} onChange={event => action('mode', event.target.value)}><option value="Undecided">Не выбрано</option><option value="AllTogether">Всё вместе</option><option value="PartialAllowed">Разрешена частичная доставка</option></select>{!canChangeDeliveryMode && <small>После первой доставки этот выбор изменить нельзя.</small>}</label>{hasActiveShipment && <p className="hint">Нельзя начать новую доставку, пока активная отправка не завершена или не отменена.</p>}</article>}
    <article className="panel"><h2>Стоимость и примечание</h2><form className="inline" onSubmit={event => { event.preventDefault(); action('fee', fee); }}><input type="number" min="0" step="0.01" value={fee} onChange={event => setFee(event.target.value)} /><button>Сохранить доставку</button></form><form className="stack-form" onSubmit={event => { event.preventDefault(); action('note', note); }}><textarea placeholder="Примечание администратора" value={note} onChange={event => setNote(event.target.value)} /><button>Сохранить примечание</button></form></article>
    {active && order.status !== 'PendingAdminApproval' && <article className="panel"><h2>Сотрудник и доставка</h2>{assignment ? <div className="assigned"><span>👷 {assignment.staffName}</span>{!hasActiveShipment && <button onClick={() => action('release')}>Отозвать задание</button>}</div> : <form className="inline" onSubmit={event => { event.preventDefault(); action('assign', workerId); }}><select required value={workerId} onChange={event => setWorkerId(event.target.value)}><option value="">Выберите сотрудника</option>{workers.map(worker => <option key={worker.id} value={worker.id}>{fullName(worker)}</option>)}</select><button>Назначить задание</button></form>}{assignment && order.readyQuantity > 0 && !hasActiveShipment && <button className="primary wide-space" onClick={() => action('shipment')}>Передать готовые товары сотруднику</button>}{shipments.map(shipment => { const next = nextShipment[shipment.status]; return <div className="shipment" key={shipment.id}><div className="row"><strong>{shipment.staffName}</strong><Status value={shipmentLabels[shipment.status]} /></div>{shipment.items.map(item => <p key={item.orderItemId}>{item.quantity} × {item.productName}</p>)}{next && <button onClick={() => action('shipmentStatus', `${shipment.id}|${next}`)}>{shipmentLabels[next]}</button>}</div>; })}</article>}
    {active && !hasActiveShipment && order.fulfillmentStatus !== 'PartiallyDelivered' && <article className="panel danger-zone"><h2>Отменить заказ</h2><form className="inline" onSubmit={event => { event.preventDefault(); if (cancelReason.trim()) action('cancel', cancelReason.trim()); }}><input required placeholder="Причина отмены" value={cancelReason} onChange={event => setCancelReason(event.target.value)} /><button>Отменить</button></form></article>}
  </div></section>;
}

function ProductImage({ product }: { product: Product }) { const [failed, setFailed] = useState(false); return product.imageReference && !failed ? <img className="product-image" src={productImageUrl(product.id)} alt={product.name} onError={() => setFailed(true)} /> : <div className="product-image placeholder">🛍️</div>; }

function ProductsScreen({ products, categories, subcategories, demo, back, busy, save, toggle, reload }: { products: Product[]; categories: Category[]; subcategories: Subcategory[]; demo: boolean; back: () => void; busy: boolean; save: (product: Product | null, data: ProductInput, image?: File) => Promise<unknown>; toggle: (product: Product) => Promise<unknown>; reload: () => Promise<void> }) {
  const [editor, setEditor] = useState<Product | 'new' | null>(null);
  const [manageCategories, setManageCategories] = useState(false);
  const [search, setSearch] = useState('');
  const [only, setOnly] = useState<'current' | 'all' | 'active' | 'неактивнаe' | 'archived'>('current');
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
    if (demo || !window.confirm(`Архивировать товар «${product.name}»?`)) return;
    setActionError('');
    try { await api.archiveProduct(product.id); await reload(); }
    catch (reason) { setActionError(reason instanceof Error ? reason.message : 'Не удалось архивировать товар.'); }
  }
  async function restore(product: Product) {
    if (demo) return;
    setActionError('');
    try { await api.restoreProduct(product.id); await reload(); }
    catch (reason) { setActionError(reason instanceof Error ? reason.message : 'Не удалось восстановить товар.'); }
  }
  async function remove(product: Product) {
    if (demo || !window.confirm(`Удалить товар «${product.name}» навсегда? Это действие нельзя отменить.`)) return;
    setActionError('');
    try {
      await api.deleteProduct(product.id);
      setSelectedIds(values => values.filter(id => id !== product.id));
      await reload();
    } catch (reason) { setActionError(reason instanceof Error ? reason.message : 'Не удалось удалить товар.'); }
  }
  async function applyBulk() {
    if (demo || !selectedIds.length) return;
    if (bulkAction === 'Move' && !bulkCategoryId) return;
    setActionError('');
    try {
      await api.applyBulkProductAction(selectedIds, bulkAction, bulkCategoryId || undefined, bulkSubcategoryId || undefined);
      setSelectedIds([]); await reload();
    } catch (reason) { setActionError(reason instanceof Error ? reason.message : 'Не удалось выполнить массовую операцию.'); }
  }

  return <section className="screen">
    <Heading title="Товары" back={back} action={<button className="heading-action" onClick={() => setEditor('new')}>＋ Добавить</button>} />
    <button className="secondary wide-space catalog-manager-button" onClick={() => setManageCategories(true)}>Управление категориями и размерами</button>
    <div className="toolbar catalog-toolbar">
      <input className="search" placeholder="Поиск по товару или коду" value={search} onChange={event => setSearch(event.target.value)} />
      <select value={categoryId} onChange={event => { setCategoryId(event.target.value); setSubcategoryId(''); }}><option value="">Все категории</option>{categories.filter(item => !item.isArchived).map(item => <option key={item.id} value={item.id}>{item.name}</option>)}</select>
      <select value={subcategoryId} onChange={event => setSubcategoryId(event.target.value)}><option value="">Все размеры</option>{availableSubcategories.filter(item => !item.isArchived).map(item => <option key={item.id} value={item.id}>{item.name} · {item.categoryName}</option>)}</select>
      <select value={only} onChange={event => setOnly(event.target.value as typeof only)}><option value="current">Текущие товары</option><option value="active">Активный</option><option value="неактивнаe">Неактивный</option><option value="archived">Архив</option><option value="all">Все, включая архив</option></select>
      <select value={stockFilter} onChange={event => setStockFilter(event.target.value as typeof stockFilter)}><option value="all">Все остатки</option><option value="in">Есть на складе</option><option value="out">Нет на складе</option><option value="low">Мало осталось</option></select>
      <button onClick={() => { setSearch(''); setCategoryId(''); setSubcategoryId(''); setOnly('current'); setStockFilter('all'); }}>Сбросить фильтры</button>
    </div>
    {actionError && <p className="danger-text">{actionError}</p>}
    {!!selectedIds.length && <div className="bulk-bar"><strong>{selectedIds.length} товаров выбрано</strong><select value={bulkAction} onChange={event => setBulkAction(event.target.value as typeof bulkAction)}><option value="Activate">Активировать</option><option value="Deactivate">Деактивировать</option><option value="Archive">Архивировать</option><option value="Restore">Восстановить из архива</option><option value="Move">Переместить в категорию</option></select>{bulkAction === 'Move' && <><select required value={bulkCategoryId} onChange={event => { setBulkCategoryId(event.target.value); setBulkSubcategoryId(''); }}><option value="">Целевая категория</option>{categories.filter(item => item.isActive && !item.isArchived).map(item => <option key={item.id} value={item.id}>{item.name}</option>)}</select><select value={bulkSubcategoryId} onChange={event => setBulkSubcategoryId(event.target.value)}><option value="">Без размера</option>{subcategories.filter(item => item.categoryId === bulkCategoryId && item.isActive && !item.isArchived).map(item => <option key={item.id} value={item.id}>{item.name}</option>)}</select></>}<button className="primary" disabled={bulkAction === 'Move' && !bulkCategoryId} onClick={applyBulk}>Применить</button><button onClick={() => setSelectedIds([])}>Отменить</button></div>}
    <div className="product-grid">{list.map(product => <article className={`product-card ${!product.isActive || product.isArchived ? 'неактивнаe' : ''}`} key={product.id}><label className="product-select"><input type="checkbox" checked={selectedIds.includes(product.id)} onChange={event => setSelectedIds(values => event.target.checked ? [...values, product.id] : values.filter(id => id !== product.id))} /> Выбрать</label><ProductImage product={product} /><div className="product-body"><div className="row"><strong>{product.name}</strong><Status value={product.isArchived ? 'Архив' : product.isActive ? 'Активный' : 'Неактивный'} tone={product.isArchived || !product.isActive ? 'danger' : 'success'} /></div><small>{product.code}</small><p>{product.categoryName || 'Без категории'}{product.subcategoryName ? ` · ${product.subcategoryName}` : ''}</p><p>{product.description}</p><div className="row"><b>{money(product.price)}</b><span className={product.isLowStock ? 'danger-text' : ''}>Склад: {product.stockQuantity} · Minimum: {product.minimumStockQuantity}</span></div><div className="card-actions"><button onClick={() => setEditor(product)}>Редактировать</button>{product.isArchived ? <button onClick={() => restore(product)}>Восстановить из архива</button> : <><button className={product.isActive ? 'danger-button' : ''} onClick={() => toggle(product)}>{product.isActive ? 'Деактивировать' : 'Активировать'}</button><button className="danger-button" onClick={() => archive(product)}>Архивировать</button></>}<button className="danger-button" onClick={() => remove(product)}>Удалить навсегда</button></div></div></article>)}</div>
    {!list.length && <Empty text="Товар не найден" />}
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
  async function createCategoryInline() { if (demo) return; const name = window.prompt('Название новой категории')?.trim(); if (!name) return; try { const created = await api.createCategory(name, categories.length); await reload(); setCategoryId(created.id); setSubcategoryId(''); } catch (reason) { window.alert(reason instanceof Error ? reason.message : 'Не удалось создать категорию.'); } }
  async function createSubcategoryInline() { if (demo || !categoryId) return; const name = window.prompt('Название нового размера / подкатегории')?.trim(); if (!name) return; try { const created = await api.createSubcategory(categoryId, name, availableSubcategories.length); await reload(); setSubcategoryId(created.id); } catch (reason) { window.alert(reason instanceof Error ? reason.message : 'Не удалось создать подкатегорию.'); } }
  function submit(event: FormEvent) { event.preventDefault(); if (!categoryId) return; save({ name: name.trim(), code: code.trim(), price: Number(price), initialStockQuantity: Number(stock), description: description.trim(), imageReference: product?.imageReference, categoryId, subcategoryId: subcategoryId || undefined, minimumStockQuantity: Number(minimumStock) }, image); }
  return <div className="modal"><form className="sheet" onSubmit={submit}><div className="sheet-title"><div><small>{product ? 'Обновить товар' : 'Новый товар'}</small><h2>{product?.name ?? 'Добавить в каталог'}</h2></div><button type="button" onClick={close}>×</button></div><label>Название<input required value={name} onChange={event => setName(event.target.value)} /></label><label>Код товара<input required disabled={!!product} value={code} onChange={event => setCode(event.target.value)} /></label><div className="two-columns"><label>Категория<select required value={categoryId} onChange={event => { setCategoryId(event.target.value); setSubcategoryId(''); }}><option value="">Выберите категорию</option>{activeCategories.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}</select><button type="button" className="text-button" onClick={createCategoryInline}>＋ Новая категория</button></label><label>Размер / подкатегория<select value={subcategoryId} onChange={event => setSubcategoryId(event.target.value)}><option value="">Без размера</option>{availableSubcategories.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}</select><button type="button" className="text-button" disabled={!categoryId} onClick={createSubcategoryInline}>＋ Новый размер</button></label></div><div className="two-columns"><label>Цена<input type="number" min="0.01" step="0.01" required value={price} onChange={event => setPrice(event.target.value)} /></label>{!product && <label>Начальный остаток<input type="number" min="0" required value={stock} onChange={event => setStock(event.target.value)} /></label>}<label>Минимальный остаток<input type="number" min="0" required value={minimumStock} onChange={event => setMinimumStock(event.target.value)} /></label></div><label>Описание<textarea required value={description} onChange={event => setDescription(event.target.value)} /></label><label>Изображение<input type="file" accept="image/jpeg,image/png,image/webp" onChange={event => setImage(event.target.files?.[0])} /><small>JPG, PNG или WEBP · максимум 5 МБ</small></label>{!activeCategories.length && <p className="danger-text">Сначала создайте активную категорию.</p>}{demo && <p className="hint">В деморежиме изменение сохраняется только на этом экране.</p>}<button className="primary" disabled={busy || !categoryId}>{product ? 'Сохранить' : 'Добавить товар'}</button></form></div>;
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
    if (demo) { setError('В деморежиме изменения категорий не сохраняются.'); return; }
    setError('');
    try { await action(); await reload(); }
    catch (reason) { setError(reason instanceof Error ? reason.message : 'Не удалось выполнить операцию.'); }
  }

  async function addCategory(event: FormEvent) {
    event.preventDefault();
    if (!categoryName.trim()) return;
    await execute(async () => { const created = await api.createCategory(categoryName.trim(), categories.length); setSelectedCategoryId(created.id); setCategoryName(''); });
  }

  async function editCategory(category: Category) {
    const name = window.prompt('Новое название категории', category.name)?.trim();
    if (!name) return;
    const sortText = window.prompt('Порядковый номер', String(category.sortOrder));
    if (sortText === null) return;
    await execute(() => api.updateCategory(category.id, name, Number(sortText)));
  }

  async function removeCategory(category: Category) {
    if (category.productCount === 0 && category.subcategoryCount === 0) {
      if (window.confirm(`Удалить категорию «${category.name}» навсегда?`)) await execute(() => api.deleteEmptyCategory(category.id));
      return;
    }
    const choice = window.prompt(
      `В категории «${category.name}» находятся товары: ${category.productCount}, подкатегории: ${category.subcategoryCount}.\n\n` +
      '1 — Перенести товары в другую категорию и удалить эту\n' +
      '2 — Архивировать только категорию\n' +
      '3 — Архивировать также товары\n' +
      '4 — Удалить товары, размеры и категорию навсегда\n\nВведите 1, 2, 3 или 4:');
    if (choice === '2') await execute(() => api.removeCategory(category.id, 'ArchiveCategory'));
    if (choice === '3') await execute(() => api.removeCategory(category.id, 'ArchiveProducts'));
    if (choice === '4' && window.confirm('Категория, размеры и доступные для удаления товары будут удалены навсегда. Продолжить?')) await execute(() => api.removeCategory(category.id, 'DeleteProducts'));
    if (choice === '1') {
      const targets = categories.filter(item => item.id !== category.id && item.isActive && !item.isArchived);
      if (!targets.length) { setError('Нет другой активной категории для переноса.'); return; }
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
    const name = window.prompt('Новое название подкатегории', subcategory.name)?.trim();
    if (!name) return;
    const sortText = window.prompt('Порядковый номер', String(subcategory.sortOrder));
    if (sortText === null) return;
    await execute(() => api.updateSubcategory(subcategory.id, name, Number(sortText)));
  }

  async function removeSubcategory(subcategory: Subcategory) {
    if (subcategory.productCount === 0) {
      if (window.confirm(`Удалить подкатегорию «${subcategory.name}» навсегда?`)) await execute(() => api.deleteEmptySubcategory(subcategory.id));
      return;
    }
    const choice = window.prompt(
      `В подкатегории «${subcategory.name}» находятся товары: ${subcategory.productCount}.\n\n` +
      '1 — Перенести товары в другую подкатегорию и удалить эту\n' +
      '2 — Архивировать только подкатегорию\n' +
      '3 — Архивировать также товары\n' +
      '4 — Удалить товары и подкатегорию навсегда\n\nВведите 1, 2, 3 или 4:');
    if (choice === '2') await execute(() => api.removeSubcategory(subcategory.id, 'ArchiveSubcategory'));
    if (choice === '3') await execute(() => api.removeSubcategory(subcategory.id, 'ArchiveProducts'));
    if (choice === '4' && window.confirm('Подкатегория и доступные для удаления товары будут удалены навсегда. Продолжить?')) await execute(() => api.removeSubcategory(subcategory.id, 'DeleteProducts'));
    if (choice === '1') {
      const targets = subcategories.filter(item => item.id !== subcategory.id && item.isActive && !item.isArchived);
      if (!targets.length) { setError('Нет другой активной подкатегории для переноса.'); return; }
      const index = Number(window.prompt(targets.map((item, i) => `${i + 1} — ${item.categoryName} / ${item.name}`).join('\n')) ?? 0) - 1;
      if (targets[index]) await execute(() => api.removeSubcategory(subcategory.id, 'MoveProducts', targets[index].id));
    }
  }

  return <div className="modal"><div className="sheet category-manager">
    <div className="sheet-title"><div><small>Структура каталога</small><h2>Категории и размеры</h2></div><button onClick={close}>×</button></div>
    {error && <p className="danger-text">{error}</p>}
    <form className="inline" onSubmit={addCategory}><input required placeholder="Новая категория" value={categoryName} onChange={event => setCategoryName(event.target.value)} /><button>Добавить</button></form>
    <button className="secondary" onClick={() => setShowArchived(value => !value)}>{showArchived ? 'Скрыть архив' : 'Показать архив'}</button>
    <div className="category-tabs">{visibleCategories.map(category => <button className={currentCategoryId === category.id ? 'active' : ''} key={category.id} onClick={() => setSelectedCategoryId(category.id)}>{category.name}{category.isArchived ? ' · Архив' : !category.isActive ? ' · Неактивный' : ''}</button>)}</div>
    {selectedCategory && <article className="panel"><div className="row"><div><h3>{selectedCategory.name}</h3><small>{selectedCategory.productCount} товаров · {selectedCategory.subcategoryCount} подкатегорий · порядок {selectedCategory.sortOrder}</small></div><Status value={selectedCategory.isArchived ? 'Архив' : selectedCategory.isActive ? 'Активный' : 'Неактивный'} tone={selectedCategory.isActive && !selectedCategory.isArchived ? 'success' : 'danger'} /></div><div className="card-actions"><button onClick={() => editCategory(selectedCategory)}>Изменить название и порядок</button>{selectedCategory.isArchived ? <button onClick={() => execute(() => api.restoreCategory(selectedCategory.id))}>Восстановить из архива</button> : <button onClick={() => execute(() => api.setCategoryActive(selectedCategory.id, !selectedCategory.isActive))}>{selectedCategory.isActive ? 'Деактивировать' : 'Активировать'}</button>}<button className="danger-button" onClick={() => removeCategory(selectedCategory)}>Удалить / архивировать</button></div></article>}
    {selectedCategory && !selectedCategory.isArchived && <form className="inline" onSubmit={addSubcategory}><input required placeholder="Новый размер / подкатегория" value={subcategoryName} onChange={event => setSubcategoryName(event.target.value)} /><button>Добавить</button></form>}
    <div className="list">{categorySubcategories.map(subcategory => <article className="list-card static" key={subcategory.id}><div className="row"><div><strong>{subcategory.name}</strong><small>{subcategory.productCount} товаров · порядок {subcategory.sortOrder}</small></div><Status value={subcategory.isArchived ? 'Архив' : subcategory.isActive ? 'Активный' : 'Неактивный'} tone={subcategory.isActive && !subcategory.isArchived ? 'success' : 'danger'} /></div><div className="card-actions"><button onClick={() => editSubcategory(subcategory)}>Изменить</button>{subcategory.isArchived ? <button onClick={() => execute(() => api.restoreSubcategory(subcategory.id))}>Восстановить из архива</button> : <button onClick={() => execute(() => api.setSubcategoryActive(subcategory.id, !subcategory.isActive))}>{subcategory.isActive ? 'Деактивировать' : 'Активировать'}</button>}<button className="danger-button" onClick={() => removeSubcategory(subcategory)}>Удалить / архивировать</button></div></article>)}{selectedCategory && !categorySubcategories.length && <Empty text="Подкатегорий нет" />}</div>
  </div></div>;
}

function StockScreen({ products, demo, back, change, history }: { products: Product[]; demo: boolean; back: () => void; change: (product: Product, action: StockAction, quantity: number, reason: string) => Promise<unknown>; history: (product: Product) => Promise<StockMovement[]> }) {
  const [selected, setSelected] = useState<Product | null>(null); const [movements, setMovements] = useState<StockMovement[]>([]); const [quantity, setQuantity] = useState('1'); const [reason, setReason] = useState('Новое поступление товара');
  async function open(product: Product) { setSelected(product); setMovements(await history(product)); }
  async function apply(action: StockAction) { if (!selected) return; await change(selected, action, Number(quantity), reason); setSelected(null); }
  return <section className="screen"><Heading title="Склад" back={back} /><div className="list">{products.map(product => <button className="list-card" key={product.id} onClick={() => open(product)}><div className="row"><strong>{product.name}</strong><Status value={product.stockQuantity === 0 ? 'Нет в наличии' : product.stockQuantity < 5 ? 'Мало осталось' : 'Норма'} tone={product.stockQuantity < 5 ? 'warning' : 'success'} /></div><div className="row meta"><span>{product.code}</span><b>{product.stockQuantity} шт.</b></div></button>)}</div>{selected && <div className="modal"><div className="sheet"><div className="sheet-title"><div><small>Управление складом</small><h2>{selected.name}</h2></div><button onClick={() => setSelected(null)}>×</button></div><div className="stock-now">Сейчас <strong>{selected.stockQuantity}</strong> шт.</div><label>Количество<input type="number" min="0" value={quantity} onChange={event => setQuantity(event.target.value)} /></label><label>Причина<input value={reason} onChange={event => setReason(event.target.value)} /></label><div className="button-grid"><button onClick={() => apply('add')}>＋ Добавить</button><button onClick={() => apply('remove')}>− Уменьшить</button><button onClick={() => apply('set')}>Установить точно</button></div><h3>Последние движения</h3>{movements.length ? <div className="movement-list">{movements.slice(0, 8).map(item => <div key={item.id}><span>{item.reason}</span><b className={item.quantityChange >= 0 ? 'positive' : 'negative'}>{item.quantityChange > 0 ? '+' : ''}{item.quantityChange}</b><small>{date(item.createdAtUtc)}</small></div>)}</div> : <p className="hint">{demo ? 'История деморежима пуста.' : 'Движений по складу нет.'}</p>}</div></div>}</section>;
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
    .map(item => `${item.productName} (${item.productCode}) — ${item.missingQuantity} шт.`)
    .join('\n');

  async function open(item: MissingProduct) {
    setSelected(item);
    setBusy(true);
    setLocalError('');

    try {
      setAffected(await loadOrders(item.productId));
    } catch (reason) {
      setLocalError(reason instanceof Error ? reason.message : 'Не удалось открыть подробности.');
    } finally {
      setBusy(false);
    }
  }

  async function copy() {
    if (!copyText) return;

    try {
      await navigator.clipboard.writeText(copyText);
      notify('Список недостающих товаров скопирован.');
    } catch {
      setLocalError('Не удалось скопировать список.');
    }
  }

  async function exportList() {
    if (!items.length || demo) return;

    setExporting(true);
    setLocalError('');

    try {
      await exportExcel();
      notify('Скачивание файла Excel началось.');
    } catch (reason) {
      setLocalError(reason instanceof Error
        ? reason.message
        : 'Не удалось скачать файл Excel.');
    } finally {
      setExporting(false);
    }
  }

  return <section className="screen">
    <Heading title="Недостающие товары" back={back} />
    <div className="button-grid">
      <button disabled={!items.length} onClick={copy}>Копировать</button>
      <button
        className="primary"
        disabled={!items.length || demo || exporting}
        onClick={exportList}
      >
        {exporting ? 'Подготовка…' : 'Экспортировать в Excel'}
      </button>
    </div>
    {demo && <p className="hint">В деморежиме реальные недостачи отсутствуют.</p>}
    {localError && <p className="danger-text">{localError}</p>}
    <div className="list">
      {items.map(item => <button className="list-card" key={item.productId} onClick={() => open(item)}>
        <div className="row">
          <strong>{item.productName}</strong>
          <Status value={`${item.missingQuantity} шт.`} tone="warning" />
        </div>
        <div className="row meta">
          <span>{item.productCode}</span>
          <b>{item.affectedOrderCount} заказов</b>
        </div>
      </button>)}
      {!items.length && <Empty text="Сейчас недостающих товаров нет" />}
    </div>
    {selected && <div className="modal"><div className="sheet">
      <div className="sheet-title">
        <div><small>Недостающий товар</small><h2>{selected.productName}</h2></div>
        <button onClick={() => setSelected(null)}>×</button>
      </div>
      <div className="stock-now">Не хватает всего: <strong>{selected.missingQuantity}</strong> шт.</div>
      {busy
        ? <span className="spinner" />
        : <div className="list">
          {affected.map(order => <button className="list-card" key={order.orderNumber} onClick={() => openOrderNumber(order.orderNumber)}>
            <div className="row">
              <strong>#{order.orderNumber}</strong>
              <Status value={`${order.missingQuantity} шт.`} tone="warning" />
            </div>
            <h3>{order.customerName}</h3>
            <small>{specialLabels[order.status]}</small>
          </button>)}
          {!affected.length && <Empty text="Связанных заказов нет" />}
        </div>}
    </div></div>}
  </section>;
}

function StockPurchasesScreen({ items, products, demo, back, search, create }: { items: StockPurchase[]; products: Product[]; demo: boolean; back: () => void; search: (from: string, to: string) => Promise<unknown>; create: (purchasedAtUtc: string, note: string, items: CreateStockPurchaseItem[]) => Promise<boolean> }) {
  const [from, setFrom] = useState(defaultFrom);
  const [to, setTo] = useState(defaultToday);
  const [editor, setEditor] = useState(false);
  const [selected, setSelected] = useState<StockPurchase | null>(null);
  return <section className="screen"><Heading title="Закупки" back={back} action={<button className="heading-action" onClick={() => setEditor(true)}>＋ Закупка</button>} /><form className="period-form" onSubmit={event => { event.preventDefault(); void search(from, to); }}><label>Начало<input type="date" required value={from} max={to} onChange={event => setFrom(event.target.value)} /></label><label>Конец<input type="date" required value={to} min={from} onChange={event => setTo(event.target.value)} /></label><button className="primary">Показать</button></form><div className="list">{items.map(item => <button className="list-card" key={item.id} onClick={() => setSelected(item)}><div className="row"><strong>{date(item.purchasedAtUtc)}</strong><b>{money(item.totalAmount)}</b></div><p>{item.note || 'Примечания нет'}</p><small>{item.items.length} товарных позиций</small></button>)}{!items.length && <Empty text="За выбранный период закупок нет" />}</div>{editor && <StockPurchaseEditor products={products} close={() => setEditor(false)} save={async (purchasedAtUtc, note, rows) => { if (await create(purchasedAtUtc, note, rows)) setEditor(false); }} />}{selected && <div className="modal"><div className="sheet"><div className="sheet-title"><div><small>{date(selected.purchasedAtUtc)}</small><h2>Детали закупки</h2></div><button onClick={() => setSelected(null)}>×</button></div>{selected.items.map(item => <div className="purchase-line" key={item.id}><div><strong>{item.productName}</strong><small>{item.productCode} · {item.quantity} × {money(item.unitCost)}</small></div><b>{money(item.lineTotal)}</b></div>)}<div className="totals"><strong>Итого <b>{money(selected.totalAmount)}</b></strong></div>{selected.note && <p className="hint">Примечание: {selected.note}</p>}</div></div>}{demo && <p className="hint">В деморежиме закупки сохраняются только в этой сессии.</p>}</section>;
}

function StockPurchaseEditor({ products, close, save }: { products: Product[]; close: () => void; save: (purchasedAtUtc: string, note: string, items: CreateStockPurchaseItem[]) => Promise<void> }) {
  const [purchasedAt, setPurchasedAt] = useState(defaultLocalDateTime);
  const [note, setNote] = useState('');
  const [rows, setRows] = useState<Array<CreateStockPurchaseItem & { key: string }>>([{ key: crypto.randomUUID(), productId: '', quantity: 1, unitCost: 0.01 }]);
  function update(key: string, changes: Partial<CreateStockPurchaseItem>) { setRows(values => values.map(value => value.key === key ? { ...value, ...changes } : value)); }
  return <div className="modal"><form className="sheet" onSubmit={event => { event.preventDefault(); void save(new Date(purchasedAt).toISOString(), note.trim(), rows.map(({ productId, quantity, unitCost }) => ({ productId, quantity, unitCost }))); }}><div className="sheet-title"><div><small>Приём товара</small><h2>Новая закупка</h2></div><button type="button" onClick={close}>×</button></div><label>Дата закупки<input type="datetime-local" required value={purchasedAt} onChange={event => setPurchasedAt(event.target.value)} /></label><label>Примечание<textarea value={note} onChange={event => setNote(event.target.value)} placeholder="Накладная или примечание поставщика" /></label><h3>Товары</h3>{rows.map((row, index) => <div className="purchase-editor-row" key={row.key}><select required value={row.productId} onChange={event => update(row.key, { productId: event.target.value })}><option value="">Выберите товар</option>{products.map(product => <option key={product.id} value={product.id} disabled={rows.some(other => other.key !== row.key && other.productId === product.id)}>{product.name} ({product.code})</option>)}</select><input aria-label="Количество" type="number" required min="1" value={row.quantity} onChange={event => update(row.key, { quantity: Number(event.target.value) })} /><input aria-label="Себестоимость единицы" type="number" required min="0.01" step="0.01" value={row.unitCost} onChange={event => update(row.key, { unitCost: Number(event.target.value) })} />{rows.length > 1 && <button type="button" onClick={() => setRows(values => values.filter(value => value.key !== row.key))}>×</button>}{index === 0 && <small>Количество · себестоимость единицы</small>}</div>)}<button type="button" className="text-button" disabled={rows.length >= products.length} onClick={() => setRows(values => [...values, { key: crypto.randomUUID(), productId: '', quantity: 1, unitCost: 0.01 }])}>＋ Строка товара</button><button className="primary">Сохранить закупку</button></form></div>;
}

function demoTurnoverReport(fromUtc: string, toUtc: string, companyId: string | undefined, companies: DeliveryCompany[]): InventoryTurnoverReport {
  return { fromUtc, toUtc, deliveryCompanyId: companyId, deliveryCompanyName: companies.find(item => item.id === companyId)?.name, products: [], totalPurchasedQuantity: 0, totalPurchaseAmount: 0, totalSoldQuantity: 0, totalSalesAmount: 0 };
}

function ReportsScreen({ report, companies, back, generate }: { report: InventoryTurnoverReport | null; companies: DeliveryCompany[]; back: () => void; generate: (from: string, to: string, companyId?: string) => Promise<unknown> }) {
  const [from, setFrom] = useState(defaultFrom);
  const [to, setTo] = useState(defaultToday);
  const [companyId, setCompanyId] = useState('');
  return <section className="screen"><Heading title="Продажи и оборот" back={back} /><form className="report-form" onSubmit={event => { event.preventDefault(); void generate(from, to, companyId || undefined); }}><div className="two-columns"><label>Начало<input type="date" required value={from} max={to} onChange={event => setFrom(event.target.value)} /></label><label>Конец<input type="date" required value={to} min={from} onChange={event => setTo(event.target.value)} /></label></div><label>Служба доставки<select value={companyId} onChange={event => setCompanyId(event.target.value)}><option value="">Все службы</option>{companies.map(company => <option key={company.id} value={company.id}>{company.name}{company.isActive ? '' : ' (неактивна)'}</option>)}</select></label><button className="primary">Сформировать отчёт</button></form>{report ? <><div className="report-summary"><div><span>Количество продаж</span><strong>{report.totalSoldQuantity}</strong></div><div><span>Сумма продаж</span><strong>{money(report.totalSalesAmount)}</strong></div><div><span>Количество закупок</span><strong>{report.totalPurchasedQuantity}</strong></div><div><span>Сумма закупок</span><strong>{money(report.totalPurchaseAmount)}</strong></div></div>{report.deliveryCompanyName && <p className="hint">Фильтр: {report.deliveryCompanyName}</p>}<div className="list">{report.products.map(product => <article className="list-card static" key={product.productId}><div className="row"><strong>{product.productName}</strong><small>{product.productCode}</small></div><div className="turnover-grid"><span>Продано <b>{product.soldQuantity}</b></span><span>Продажи <b>{money(product.salesAmount)}</b></span><span>Закуплено <b>{product.purchasedQuantity}</b></span><span>Закупки <b>{money(product.purchaseAmount)}</b></span></div></article>)}{!report.products.length && <Empty text="За выбранный период оборота нет" />}</div></> : <Empty text="Выберите период и службу, затем сформируйте отчёт" />}</section>;
}

function DeliveryCompaniesScreen({ items, back, add, rename, toggle }: { items: DeliveryCompany[]; back: () => void; add: (name: string) => Promise<boolean>; rename: (company: DeliveryCompany, name: string) => Promise<boolean>; toggle: (company: DeliveryCompany) => Promise<unknown> }) {
  const [newName, setNewName] = useState('');
  const [editing, setEditing] = useState<DeliveryCompany | null>(null);
  const [editName, setEditName] = useState('');
  return <section className="screen"><Heading title="Службы доставки" back={back} /><form className="inline" onSubmit={event => { event.preventDefault(); const name = newName.trim(); if (name) void add(name).then(ok => { if (ok) setNewName(''); }); }}><input required maxLength={100} placeholder="Название новой службы" value={newName} onChange={event => setNewName(event.target.value)} /><button className="primary">Добавить</button></form><div className="list">{items.map(company => <article className="list-card static" key={company.id}><div className="row"><h3>🚚 {company.name}</h3><Status value={company.isActive ? 'Активный' : 'Неактивный'} tone={company.isActive ? 'success' : 'danger'} /></div><small>Добавлено: {date(company.createdAtUtc)}</small><div className="card-actions"><button onClick={() => { setEditing(company); setEditName(company.name); }}>Изменить название</button><button className={company.isActive ? 'danger-button' : ''} onClick={() => toggle(company)}>{company.isActive ? 'Деактивировать' : 'Активировать'}</button></div></article>)}{!items.length && <Empty text="Службы доставки не добавлены" />}</div>{editing && <div className="modal"><form className="sheet" onSubmit={event => { event.preventDefault(); const name = editName.trim(); if (name) void rename(editing, name).then(ok => { if (ok) setEditing(null); }); }}><div className="sheet-title"><h2>Изменить название службы</h2><button type="button" onClick={() => setEditing(null)}>×</button></div><label>Название<input required maxLength={100} value={editName} onChange={event => setEditName(event.target.value)} /></label><button className="primary">Сохранить</button></form></div>}</section>;
}

function StaffScreen({ staff, back, add, toggle }: { staff: Staff[]; back: () => void; add: (data: { telegramUserId: number; firstName: string; lastName?: string; username?: string }) => Promise<unknown>; toggle: (worker: Staff) => Promise<unknown> }) {
  const [form, setForm] = useState(false); const workers = staff.filter(item => item.role === 'Worker');
  return <section className="screen"><Heading title="Сотрудники" back={back} action={<button className="heading-action" onClick={() => setForm(true)}>＋ Сотрудник</button>} /><div className="summary-row"><div><strong>{workers.filter(item => item.isActive).length}</strong><span>Активные</span></div><div><strong>{workers.filter(item => !item.isActive).length}</strong><span>Неактивные</span></div></div><div className="list">{workers.map(worker => <article className="list-card static" key={worker.id}><div className="row"><h3>👷 {fullName(worker)}</h3><Status value={worker.isActive ? 'Активный' : 'Неактивный'} tone={worker.isActive ? 'success' : 'danger'} /></div><p>{worker.username ? `@${worker.username}` : `Telegram ID: ${worker.telegramUserId}`}</p><button className="text-button" onClick={() => toggle(worker)}>{worker.isActive ? 'Деактивировать' : 'Активировать снова'}</button></article>)}{!workers.length && <Empty text="Сотрудники не добавлены" />}</div>{form && <WorkerForm close={() => setForm(false)} save={async data => { await add(data); setForm(false); }} />}</section>;
}

function WorkerForm({ close, save }: { close: () => void; save: (data: { telegramUserId: number; firstName: string; lastName?: string; username?: string }) => Promise<void> }) {
  const [id, setId] = useState(''); const [first, setFirst] = useState(''); const [last, setLast] = useState(''); const [username, setUsername] = useState('');
  return <div className="modal"><form className="sheet" onSubmit={event => { event.preventDefault(); save({ telegramUserId: Number(id), firstName: first.trim(), lastName: last.trim() || undefined, username: username.replace('@', '').trim() || undefined }); }}><div className="sheet-title"><h2>Новый сотрудник</h2><button type="button" onClick={close}>×</button></div><label>Telegram ID<input type="number" required value={id} onChange={event => setId(event.target.value)} /></label><label>Имя<input required value={first} onChange={event => setFirst(event.target.value)} /></label><label>Фамилия<input value={last} onChange={event => setLast(event.target.value)} /></label><label>Имя пользователя<input value={username} onChange={event => setUsername(event.target.value)} /></label><button className="primary">Добавить</button></form></div>;
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
      setMessage('Новая одноразовая ссылка создана.');
    } catch (reason) {
      setMessage(reason instanceof Error ? reason.message : 'Не удалось создать ссылку.');
    } finally {
      setBusy(false);
    }
  }

  async function copyInvite() {
    if (!url) return;
    try {
      await navigator.clipboard.writeText(url);
      setMessage('Ссылка скопирована.');
    } catch {
      setMessage('Выделите ссылку и скопируйте её вручную.');
    }
  }

  return <section className="screen">
    <Heading title="Ссылки-приглашения" back={back} />
    <article className="panel invite-panel">
      <span className="invite-icon">🔗</span>
      <h2>Приглашение нового клиента</h2>
      <p>Клиент перейдёт по ссылке и нажмёт кнопку START. Ссылка сработает только для первого аккаунта Telegram.</p>
      <button className="primary wide-space" disabled={busy} onClick={createInvite}>
        {busy ? 'Создание…' : 'Создать новую ссылку'}
      </button>
      {url && <div className="invite-result">
        <input readOnly value={url} onFocus={event => event.currentTarget.select()} />
        <div className="invite-actions">
          <button onClick={copyInvite}>📋 Копировать</button>
          <a href={`https://t.me/share/url?url=${encodeURIComponent(url)}`} target="_blank" rel="noreferrer">Поделиться в Telegram</a>
        </div>
      </div>}
      {message && <small>{message}</small>}
      {demo && <p className="hint">Созданная в деморежиме ссылка не записывается в базу данных.</p>}
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
      `Закрыть доступ к боту для клиента ${fullName(customer)}?\n\n` +
      'История заказов не будет удалена. Клиент сможет войти снова только по новой ссылке-приглашению.');

    if (!confirmed)
      return;

    if (await block(customer))
      setSelected(null);
  }

  return <section className="screen">
    <Heading title="Клиенты" back={back} />
    <input className="search" placeholder="Имя, телефон или username" value={search} onChange={event => setSearch(event.target.value)} />
    <div className="list top-gap">
      {list.map(customer => <article className="list-card customer-card" key={customer.id}>
        <button className="customer-open" onClick={() => open(customer)}>
          <div className="row">
            <h3>👤 {fullName(customer)}</h3>
            <b>{orders.filter(item => item.customerId === customer.id).length || ''}</b>
          </div>
          <p>📞 {customer.phoneNumber || 'Номер не указан'}</p>
          <p>📍 {customer.deliveryAddress || 'Адрес не указан'}</p>
        </button>
        <button className="customer-block" aria-label={`Закрыть доступ к боту для клиента ${fullName(customer)}`} title="Закрыть доступ к боту" onClick={() => confirmBlock(customer)}>×</button>
      </article>)}
      {!list.length && <Empty text="Клиенты по вашему запросу не найдены" />}
    </div>
    {selected && <div className="modal"><div className="sheet"><div className="sheet-title"><div><small>Профиль клиента</small><h2>{fullName(selected)}</h2></div><button onClick={() => setSelected(null)}>×</button></div><a href={`tel:${selected.phoneNumber}`}>📞 {selected.phoneNumber || 'Номер не указан'}</a><p>📍 {selected.deliveryAddress || 'Адрес не указан'}</p>{selected.username && <p>Telegram: @{selected.username}</p>}<h3>Заказы ({customerOrders.length})</h3><div className="movement-list">{customerOrders.map(order => <div key={order.id}><span>#{order.orderNumber} · {orderLabels[order.status]}</span><b>{money(order.totalAmount)}</b><small>{date(order.createdAtUtc)}</small></div>)}</div>{demo && <p className="hint">Демонстрационные данные клиента.</p>}</div></div>}
  </section>;
}

function WorkerPanel({ assignments, shipments, loading, demo, error, onRefresh, onChange }: { assignments: Assignment[]; shipments: Shipment[]; loading: boolean; demo: boolean; error: string; onRefresh: () => void; onChange: (shipment: Shipment, status: ShipmentStatus) => void }) {
  const [tab, setTab] = useState<'active' | 'history'>('active');
  const active = useMemo(() => shipments.filter(item => !['Delivered', 'Cancelled'].includes(item.status)), [shipments]);
  const history = useMemo(() => shipments.filter(item => ['Delivered', 'Cancelled'].includes(item.status)), [shipments]);
  const pending = useMemo(() => assignments.filter(assignment => !shipments.some(shipment => shipment.orderNumber === assignment.orderNumber && shipment.status !== 'Cancelled')), [assignments, shipments]);
  const list = tab === 'active' ? active : history;
  const activeCount = active.length + pending.length;
  return <main className="app-shell"><Header demo={demo} subtitle="Панель сотрудника" />{error && <div className="toast error">{error}</div>}<section className="screen"><div className="hero"><span>Мои задания</span><h1>{activeCount} активных заданий</h1><p>Здесь отображаются только назначенные вам заказы.</p><button className="refresh-button" disabled={loading} onClick={onRefresh}>↻ Обновить</button></div><div className="tabs"><button className={tab === 'active' ? 'active' : ''} onClick={() => setTab('active')}>Активные ({activeCount})</button><button className={tab === 'history' ? 'active' : ''} onClick={() => setTab('history')}>История ({history.length})</button></div><div className="list top-gap">{tab === 'active' && pending.map(item => <article className="worker-card pending-task" key={item.id}><div className="row"><h2>#{item.orderNumber}</h2><Status value="Назначено" tone="info" /></div><p>Администратор назначил этот заказ вам.</p><small>Когда товары будут готовы, здесь появятся данные доставки.</small></article>)}{list.map(shipment => { const next = nextShipment[shipment.status]; return <article className="worker-card" key={shipment.id}><div className="row"><h2>#{shipment.orderNumber}</h2><Status value={shipmentLabels[shipment.status]} tone={shipment.status === 'Delivered' ? 'success' : 'info'} /></div><div className="worker-links"><a href={`tel:${shipment.phoneNumber}`}>📞 Позвонить</a><a target="_blank" rel="noreferrer" href={shipment.latitude && shipment.longitude ? `https://maps.google.com/?q=${shipment.latitude},${shipment.longitude}` : `https://maps.google.com/?q=${encodeURIComponent(shipment.deliveryAddress)}`}>📍 Карта</a></div><p>{shipment.deliveryAddress}</p><div className="products-box">{shipment.items.map(item => <div key={item.orderItemId}><strong>{item.quantity} ×</strong> {item.productName}</div>)}</div>{next && <button className="primary" disabled={loading} onClick={() => onChange(shipment, next)}>{shipmentLabels[next]}</button>}</article>; })}{!list.length && !(tab === 'active' && pending.length) && <Empty text={tab === 'active' ? 'Активных заданий нет' : 'История пуста'} />}</div></section>{loading && <div className="busy"><span className="spinner" /></div>}</main>;
}

function BottomNav({ section, open }: { section: Section; open: (value: Section) => void }) { return <nav className="bottom-nav"><button className={section === 'home' ? 'active' : ''} onClick={() => open('home')}><span>⌂</span>Главная</button><button className={section === 'orders' ? 'active' : ''} onClick={() => open('orders')}><span>▣</span>Заказы</button><button className={section === 'products' ? 'active' : ''} onClick={() => open('products')}><span>◇</span>Товары</button><button className={section === 'staff' ? 'active' : ''} onClick={() => open('staff')}><span>♙</span>Сотрудники</button></nav>; }

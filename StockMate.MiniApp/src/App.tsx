import { useEffect, useState, type FormEvent } from 'react';
import { api } from './api';
import type { Assignment, Customer, Order, Product, Shipment, ShipmentStatus, Staff } from './types';

type Section = 'home' | 'orders' | 'products' | 'stock' | 'staff' | 'customers' | 'history';
type OrderFilter = 'new' | 'active' | 'partial' | 'special' | 'completed' | 'cancelled';
type TelegramWebApp = { ready: () => void; expand: () => void; initData: string; colorScheme: 'light' | 'dark' };
declare global { interface Window { Telegram?: { WebApp: TelegramWebApp } } }

const categories: Array<{ id: Section; icon: string; title: string; description: string }> = [
  { id: 'orders', icon: '📦', title: 'Sifarişlər', description: 'Yeni, aktiv və tamamlanan sifarişlər' },
  { id: 'products', icon: '🛍️', title: 'Məhsullar', description: 'Kataloq, qiymətlər və şəkillər' },
  { id: 'stock', icon: '📊', title: 'Stok', description: 'Anbar miqdarı və hərəkətlər' },
  { id: 'staff', icon: '👷', title: 'İşçilər', description: 'İşçilər və aktiv tapşırıqlar' },
  { id: 'customers', icon: '👥', title: 'Müştərilər', description: 'Müştəri məlumatları və sifarişləri' },
  { id: 'history', icon: '📜', title: 'Tarixçə', description: 'Tamamlanan və ləğv edilən sifarişlər' },
];

const demoOrders: Order[] = [
  { id:'1',orderNumber:1048,customerId:'c1',phoneNumber:'077 329 08 78',deliveryAddress:'Nərimanov, Bakı',status:'PendingAdminApproval',deliveryMode:'Undecided',items:[{id:'i1',productName:'Coca-Cola 1 L',unitPrice:1.8,totalQuantity:2,readyQuantity:2,deliveredQuantity:0,cancelledQuantity:0,remainingQuantity:2,lineTotal:3.6}],productsTotal:3.6,totalAmount:3.6,containsSpecialOrder:false,fulfillmentStatus:'Waiting',remainingQuantity:2,readyQuantity:2,createdAtUtc:new Date().toISOString()},
  { id:'2',orderNumber:1046,customerId:'c2',phoneNumber:'050 555 11 22',deliveryAddress:'Yasamal, Bakı',status:'Preparing',deliveryMode:'PartialAllowed',items:[{id:'i2',productName:'Fanta 1 L',unitPrice:2,totalQuantity:5,readyQuantity:3,deliveredQuantity:0,cancelledQuantity:0,remainingQuantity:5,lineTotal:10}],productsTotal:10,totalAmount:10,containsSpecialOrder:true,fulfillmentStatus:'Waiting',remainingQuantity:5,readyQuantity:3,createdAtUtc:new Date().toISOString()},
];

const demoProducts: Product[] = [
  {id:'p1',name:'Coca-Cola 1 L',code:'CC-1L',price:1.8,stockQuantity:12,description:'Qazlı sərinləşdirici içki',isActive:true},
  {id:'p2',name:'Fanta 1 L',code:'FN-1L',price:2,stockQuantity:4,description:'Portağal dadlı içki',isActive:true},
];

const demoCustomers: Customer[] = [
  {id:'c1',telegramUserId:123456789,firstName:'Yunus',lastName:'Məhərrəmli',username:'yunus',phoneNumber:'077 329 08 78',deliveryAddress:'Nərimanov, Bakı'},
  {id:'c2',telegramUserId:987654321,firstName:'Aysel',lastName:'Əliyeva',phoneNumber:'050 555 11 22',deliveryAddress:'Yasamal, Bakı'},
];

function orderStatus(status: Order['status']) {
  return ({PendingAdminApproval:'Yeni',Confirmed:'Təsdiqlənib',Preparing:'Hazırlanır',Delivering:'Çatdırılmada',Completed:'Tamamlanıb',Cancelled:'Ləğv edilib'} as const)[status];
}
function shipmentStatus(status: ShipmentStatus) {
  return ({Assigned:'Təyin edilib',Preparing:'Hazırlanır',Ready:'Hazırdır',OutForDelivery:'Çatdırılmada',Delivered:'Çatdırılıb',Cancelled:'Ləğv edilib'} as const)[status];
}
function nextShipmentStatus(status: ShipmentStatus): ShipmentStatus | null {
  return ({Assigned:'Preparing',Preparing:'Ready',Ready:'OutForDelivery',OutForDelivery:'Delivered',Delivered:null,Cancelled:null} as const)[status];
}
function matchesOrderFilter(order: Order, filter: OrderFilter) {
  if (filter === 'new') return order.status === 'PendingAdminApproval';
  if (filter === 'active') return ['Confirmed', 'Preparing', 'Delivering'].includes(order.status);
  if (filter === 'partial') return order.fulfillmentStatus === 'PartiallyDelivered';
  if (filter === 'special') return order.containsSpecialOrder;
  if (filter === 'completed') return order.status === 'Completed';
  return order.status === 'Cancelled';
}

export default function App() {
  const [identity, setIdentity] = useState<Staff | null>(null);
  const [demoMode, setDemoMode] = useState(false);
  const [section, setSection] = useState<Section>('home');
  const [orderFilter, setOrderFilter] = useState<OrderFilter>('new');
  const [orders, setOrders] = useState<Order[]>([]);
  const [products, setProducts] = useState<Product[]>([]);
  const [staff, setStaff] = useState<Staff[]>([]);
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [workerShipments, setWorkerShipments] = useState<Shipment[]>([]);
  const [selectedOrder, setSelectedOrder] = useState<Order | null>(null);
  const [assignment, setAssignment] = useState<Assignment | null>(null);
  const [shipments, setShipments] = useState<Shipment[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [showWorkerForm, setShowWorkerForm] = useState(false);

  const isWorkerDemo = typeof window !== 'undefined' && new URLSearchParams(window.location.search).get('role') === 'worker';
  const isWorker = identity?.role === 'Worker' || (demoMode && isWorkerDemo);

  useEffect(() => {
    const webApp = window.Telegram?.WebApp;
    webApp?.ready(); webApp?.expand();
    if (webApp) document.documentElement.dataset.telegramTheme = webApp.colorScheme;
    if (!webApp?.initData) {
      setDemoMode(true); setOrders(demoOrders); setProducts(demoProducts); setCustomers(demoCustomers); setLoading(false); return;
    }
    api.authenticate()
      .then(async (user) => {
        setIdentity(user);
        if (user.role === 'Worker') setWorkerShipments(await api.getWorkerShipments());
      })
      .catch((reason: Error) => setError(reason.message))
      .finally(() => setLoading(false));
  }, []);

  useEffect(() => {
    if (section === 'history' && orderFilter !== 'completed' && orderFilter !== 'cancelled') {
      setOrderFilter('completed');
    }
    if (section === 'orders' && (orderFilter === 'completed' || orderFilter === 'cancelled')) {
      setOrderFilter('new');
    }
  }, [section, orderFilter]);

  useEffect(() => {
    if (demoMode || identity?.role !== 'Admin') return;
    setLoading(true); setError('');
    const load = section === 'products' || section === 'stock' ? api.getProducts().then((x) => setProducts(x.items))
      : section === 'staff' ? api.getStaff().then(setStaff)
      : section === 'customers' ? api.getCustomers().then(setCustomers)
      : section === 'orders' ? Promise.all([api.getNewOrders(), api.getActiveOrders(), api.getOrderHistory(), api.getStaff(), api.getCustomers()]).then(([newOrders, activeOrders, historyOrders, staffItems, customerItems]) => { setOrders([...newOrders, ...activeOrders, ...historyOrders]); setStaff(staffItems); setCustomers(customerItems); })
      : section === 'history' ? Promise.all([api.getOrderHistory(), api.getStaff(), api.getCustomers()]).then(([historyOrders, staffItems, customerItems]) => { setOrders(historyOrders); setStaff(staffItems); setCustomers(customerItems); })
      : Promise.resolve();
    load.catch((reason: Error) => setError(reason.message)).finally(() => setLoading(false));
  }, [section, identity, demoMode]);

  async function run(action: () => Promise<void>, message: string) {
    setError(''); setNotice(''); setLoading(true);
    try { await action(); setNotice(message); }
    catch (reason) { setError(reason instanceof Error ? reason.message : 'Əməliyyat alınmadı.'); }
    finally { setLoading(false); }
  }

  async function openOrder(order: Order) {
    setSelectedOrder(order); setAssignment(null); setShipments([]);
    if (demoMode) return;
    setLoading(true);
    try {
      const [detail, currentAssignment, currentShipments] = await Promise.all([
        api.getOrder(order.orderNumber), api.getAssignment(order.orderNumber), api.getShipments(order.orderNumber),
      ]);
      setSelectedOrder(detail); setAssignment(currentAssignment); setShipments(currentShipments);
    } catch (reason) { setError(reason instanceof Error ? reason.message : 'Sifariş açıla bilmədi.'); }
    finally { setLoading(false); }
  }

  if (loading && !identity && !demoMode) return <div className="startup"><span className="spinner"/><strong>StockMate açılır…</strong></div>;
  if (error && !identity && !demoMode) return <div className="startup error-screen"><span>⚠️</span><strong>Giriş mümkün olmadı</strong><p>{error}</p></div>;
  if (isWorker) return <WorkerPanel shipments={workerShipments} demo={demoMode} loading={loading} error={error} onChange={async (shipment, status) => run(async () => { if (!demoMode) { const updated = await api.changeShipmentStatus(shipment.id, status); setWorkerShipments((items) => items.map((item) => item.id === updated.id ? updated : item)); } }, 'Status yeniləndi.')} />;

  return (
    <main className="miniapp-shell">
      <header className="app-header"><div className="brand-mark">S</div><div className="brand-copy"><strong>StockMate</strong><span>Admin paneli</span></div><span className="connection-state">{demoMode ? 'Demo' : 'Telegram'}</span></header>
      {(error || notice) && <div className={error ? 'toast error' : 'toast'} role="status">{error || notice}<button onClick={() => {setError('');setNotice('')}}>×</button></div>}
      {section === 'home' && <Home onOpen={setSection} />}
      {(section === 'orders' || section === 'history') && !selectedOrder && <Orders orders={orders} customers={customers} filter={orderFilter} onFilter={setOrderFilter} onBack={() => setSection('home')} onOpen={openOrder} history={section === 'history'} />}
      {selectedOrder && <OrderDetail order={selectedOrder} customer={customers.find((x) => x.id === selectedOrder.customerId)} assignment={assignment} shipments={shipments} workers={staff.filter((x) => x.role === 'Worker' && x.isActive)} demo={demoMode} onBack={() => setSelectedOrder(null)} onAction={async (action, value) => {
        if (demoMode) return setNotice('Demo rejimində məlumat dəyişdirilmir.');
        await run(async () => {
          if (action === 'confirm') await api.confirmOrder(selectedOrder.orderNumber);
          if (action === 'assign') { const current = await api.assignOrder(selectedOrder.orderNumber, value!); setAssignment(current); }
          if (action === 'mode') await api.setDeliveryMode(selectedOrder.orderNumber, value as 'AllTogether' | 'PartialAllowed');
          if (action === 'shipment' && assignment) { const created = await api.createShipment(selectedOrder.orderNumber, assignment.staffId); setShipments((x) => [...x, created]); }
          if (action === 'shipmentStatus' && value) { const [shipmentId, status] = value.split('|'); const updated = await api.changeShipmentStatus(shipmentId, status as ShipmentStatus); setShipments((items) => items.map((item) => item.id === shipmentId ? updated : item)); }
          const refreshed = await api.getOrder(selectedOrder.orderNumber); setSelectedOrder(refreshed);
        }, 'Sifariş yeniləndi.');
      }} />}
      {section === 'products' && <Products products={products} onBack={() => setSection('home')} />}
      {section === 'stock' && <Stock products={products} demo={demoMode} onBack={() => setSection('home')} onChange={(product, action, quantity, reason) => run(async () => { if (!demoMode) await api.changeStock(product.id, action, quantity, reason); setProducts((items) => items.map((item) => item.id === product.id ? {...item,stockQuantity:action === 'set' ? quantity : item.stockQuantity + (action === 'add' ? quantity : -quantity)} : item)); }, 'Stok yeniləndi.')} />}
      {section === 'staff' && <StaffList staff={staff} onBack={() => setSection('home')} onAdd={() => setShowWorkerForm(true)} onToggle={(worker) => run(async () => { if (!demoMode) await api.setWorkerActive(worker.id, !worker.isActive); setStaff((items) => items.map((item) => item.id === worker.id ? {...item,isActive:!item.isActive} : item)); }, 'İşçi statusu yeniləndi.')} />}
      {showWorkerForm && <WorkerForm onClose={() => setShowWorkerForm(false)} onSave={(data) => run(async () => { if (!demoMode) { const created = await api.createWorker(data); setStaff((x) => [...x, created]); } setShowWorkerForm(false); }, 'İşçi əlavə edildi.')} />}
      {section === 'customers' && <Customers customers={customers} onBack={() => setSection('home')} />}
      <BottomNav section={section} onOpen={(next) => {setSelectedOrder(null);setSection(next)}} />
      {loading && <div className="busy"><span className="spinner"/></div>}
    </main>
  );
}

function Home({onOpen}:{onOpen:(section:Section)=>void}) { return <section className="screen"><div className="screen-heading"><span>İdarəetmə</span><h1>Kateqoriyalar</h1><p>Lazım olan bölməni seçin.</p></div><div className="category-list">{categories.map((category)=><button className="category-item" key={category.id} onClick={()=>onOpen(category.id)}><span className="category-icon">{category.icon}</span><span className="category-copy"><strong>{category.title}</strong><small>{category.description}</small></span><span className="category-arrow">›</span></button>)}</div></section> }

function Heading({title,onBack}:{title:string;onBack:()=>void}) { return <div className="subpage-heading"><button className="back-button" onClick={onBack}>‹</button><div><span>İdarəetmə</span><h1>{title}</h1></div></div> }

function Orders({orders,customers,filter,onFilter,onBack,onOpen,history}:{orders:Order[];customers:Customer[];filter:OrderFilter;onFilter:(x:OrderFilter)=>void;onBack:()=>void;onOpen:(x:Order)=>void;history:boolean}) {
  const filters: Array<{id:OrderFilter;label:string}> = history ? [{id:'completed',label:'Tamamlanan'},{id:'cancelled',label:'Ləğv edilən'}] : [{id:'new',label:'Yeni'},{id:'active',label:'Aktiv'},{id:'partial',label:'Natamam'},{id:'special',label:'Xüsusi'}];
  const visible = orders.filter((order) => matchesOrderFilter(order, filter));
  return <section className="screen"><Heading title={history?'Tarixçə':'Sifarişlər'} onBack={onBack}/><div className="filter-grid">{filters.map((item)=><button className={filter===item.id?'filter-item selected':'filter-item'} onClick={()=>onFilter(item.id)} key={item.id}><span>{item.label}</span><strong>{orders.filter((order)=>matchesOrderFilter(order,item.id)).length}</strong></button>)}</div><div className="list-heading"><h2>{filters.find((x)=>x.id===filter)?.label}</h2></div><div className="order-list">{visible.length?visible.map((order)=>{const customer=customers.find((x)=>x.id===order.customerId);return <button className="order-item" key={order.id} onClick={()=>onOpen(order)}><span className="order-topline"><strong>#{order.orderNumber}</strong><em>{orderStatus(order.status)}</em></span><span className="order-customer">{customer ? `${customer.firstName} ${customer.lastName ?? ''}`.trim() : order.phoneNumber}</span><span className="order-meta"><span>📍 {order.deliveryAddress}</span><b>{order.totalAmount.toFixed(2)} AZN</b></span></button>}):<Empty text="Bu bölmədə sifariş yoxdur"/>}</div></section>
}

function OrderDetail({order,customer,assignment,shipments,workers,demo,onBack,onAction}:{order:Order;customer?:Customer;assignment:Assignment|null;shipments:Shipment[];workers:Staff[];demo:boolean;onBack:()=>void;onAction:(action:'confirm'|'assign'|'mode'|'shipment'|'shipmentStatus',value?:string)=>void}) {
  const [workerId,setWorkerId]=useState('');
  return <section className="screen"><Heading title={`Sifariş #${order.orderNumber}`} onBack={onBack}/><div className="detail-stack"><article className="detail-card"><div className="detail-title"><strong>{orderStatus(order.status)}</strong><span>{new Date(order.createdAtUtc).toLocaleString('az-AZ')}</span></div>{customer&&<h2>👤 {customer.firstName} {customer.lastName}</h2>}{customer?.username&&<p>@{customer.username}</p>}<p>📞 {order.phoneNumber}</p><p>📍 {order.deliveryAddress}</p></article><article className="detail-card"><h2>Məhsullar</h2>{order.items.map((item)=><div className="product-line" key={item.id}><div><strong>{item.productName}</strong><span>{item.totalQuantity} × {item.unitPrice.toFixed(2)} AZN</span></div><div><b>{item.lineTotal.toFixed(2)} AZN</b><small>Hazır {item.readyQuantity} · Qalıb {item.remainingQuantity}</small></div></div>)}<div className="total-line"><span>Yekun</span><strong>{order.totalAmount.toFixed(2)} AZN</strong></div></article>{order.status==='PendingAdminApproval'&&<button className="primary-action" onClick={()=>onAction('confirm')}>✅ Sifarişi təsdiqlə</button>}{order.status!=='PendingAdminApproval'&&order.status!=='Completed'&&order.status!=='Cancelled'&&<article className="detail-card"><h2>İşçi və göndəriş</h2><p>{assignment?`👷 ${assignment.staffName}`:'İşçi təyin edilməyib'}</p>{!demo&&workers.length===0&&<small>İşçilər bölməsindən aktiv işçi əlavə edin.</small>}{workers.length>0&&<div className="inline-form"><select value={workerId} onChange={(e)=>setWorkerId(e.target.value)}><option value="">İşçi seçin</option>{workers.map((worker)=><option key={worker.id} value={worker.id}>{worker.firstName} {worker.lastName}</option>)}</select><button disabled={!workerId} onClick={()=>onAction('assign',workerId)}>Təyin et</button></div>}{order.containsSpecialOrder&&<div className="split-actions"><button onClick={()=>onAction('mode','AllTogether')}>Hamısını gözlə</button><button onClick={()=>onAction('mode','PartialAllowed')}>Hissə-hissə</button></div>}{assignment&&<button className="primary-action compact" onClick={()=>onAction('shipment')}>Göndəriş yarat</button>}</article>}{shipments.map((shipment)=>{const next=nextShipmentStatus(shipment.status);return <article className="detail-card" key={shipment.id}><div className="detail-title"><strong>🚚 {shipmentStatus(shipment.status)}</strong><span>{shipment.staffName}</span></div>{shipment.items.map((item)=><p key={item.orderItemId}>{item.quantity} × {item.productName}</p>)}{next&&<button className="primary-action compact" onClick={()=>onAction('shipmentStatus',`${shipment.id}|${next}`)}>{shipmentStatus(next)}</button>}</article>})}</div></section>
}

function Products({products,onBack}:{products:Product[];onBack:()=>void}) { return <section className="screen"><Heading title="Məhsullar" onBack={onBack}/><div className="order-list">{products.length?products.map((product)=><article className="order-item static" key={product.id}><span className="order-topline"><strong>{product.name}</strong><em>{product.isActive?'Aktiv':'Passiv'}</em></span><span className="order-customer">{product.price.toFixed(2)} AZN</span><span className="order-meta"><span>{product.code}</span><b>Stok: {product.stockQuantity}</b></span></article>):<Empty text="Məhsul yoxdur"/>}</div></section> }

function Stock({products,demo,onBack,onChange}:{products:Product[];demo:boolean;onBack:()=>void;onChange:(p:Product,a:'add'|'remove'|'set',q:number,r:string)=>void}) { const [selected,setSelected]=useState<Product|null>(null); const [quantity,setQuantity]=useState(1); const [reason,setReason]=useState('Yeni mal qəbulu'); return <section className="screen"><Heading title="Stok" onBack={onBack}/><div className="order-list">{products.map((product)=><button className="order-item" key={product.id} onClick={()=>setSelected(product)}><span className="order-topline"><strong>{product.name}</strong><em>{product.stockQuantity===0?'Yoxdur':product.stockQuantity<5?'Az qalıb':'Normal'}</em></span><span className="order-meta"><span>{product.code}</span><b>{product.stockQuantity} ədəd</b></span></button>)}</div>{selected&&<div className="modal"><div className="modal-sheet"><div className="detail-title"><strong>{selected.name}</strong><button onClick={()=>setSelected(null)}>×</button></div><label>Miqdar<input type="number" min="0" value={quantity} onChange={(e)=>setQuantity(Number(e.target.value))}/></label><label>Səbəb<input value={reason} onChange={(e)=>setReason(e.target.value)}/></label><div className="split-actions"><button onClick={()=>onChange(selected,'add',quantity,reason)}>Əlavə et</button><button onClick={()=>onChange(selected,'remove',quantity,reason)}>Çıx</button><button onClick={()=>onChange(selected,'set',quantity,reason)}>Təyin et</button></div>{demo&&<small>Demo rejimi: dəyişiklik yalnız ekranda görünür.</small>}</div></div>}</section> }

function StaffList({staff,onBack,onAdd,onToggle}:{staff:Staff[];onBack:()=>void;onAdd:()=>void;onToggle:(x:Staff)=>void}) { const workers=staff.filter((x)=>x.role==='Worker'); return <section className="screen"><Heading title="İşçilər" onBack={onBack}/><button className="primary-action" onClick={onAdd}>➕ Yeni işçi</button><div className="order-list spaced">{workers.length?workers.map((worker)=><article className="order-item static" key={worker.id}><span className="order-topline"><strong>👷 {worker.firstName} {worker.lastName}</strong><em>{worker.isActive?'Aktiv':'Passiv'}</em></span><span className="order-meta"><span>{worker.username?`@${worker.username}`:`ID: ${worker.telegramUserId}`}</span><button className="text-action" onClick={()=>onToggle(worker)}>{worker.isActive?'Passiv et':'Aktiv et'}</button></span></article>):<Empty text="Hələ işçi əlavə edilməyib"/>}</div></section> }

function WorkerForm({onClose,onSave}:{onClose:()=>void;onSave:(x:{telegramUserId:number;firstName:string;lastName?:string;username?:string})=>void}) { const [id,setId]=useState('');const [first,setFirst]=useState('');const [last,setLast]=useState('');const [username,setUsername]=useState('');function submit(e:FormEvent){e.preventDefault();onSave({telegramUserId:Number(id),firstName:first,lastName:last||undefined,username:username.replace('@','')||undefined})} return <div className="modal"><form className="modal-sheet" onSubmit={submit}><div className="detail-title"><strong>Yeni işçi</strong><button type="button" onClick={onClose}>×</button></div><label>Telegram ID<input type="number" required value={id} onChange={(e)=>setId(e.target.value)}/></label><label>Ad<input required value={first} onChange={(e)=>setFirst(e.target.value)}/></label><label>Soyad<input value={last} onChange={(e)=>setLast(e.target.value)}/></label><label>Username<input value={username} onChange={(e)=>setUsername(e.target.value)}/></label><button className="primary-action">Əlavə et</button></form></div> }

function Customers({customers,onBack}:{customers:Customer[];onBack:()=>void}) { return <section className="screen"><Heading title="Müştərilər" onBack={onBack}/><div className="order-list">{customers.length?customers.map((customer)=><article className="order-item static" key={customer.id}><span className="order-topline"><strong>👤 {customer.firstName} {customer.lastName}</strong></span><span className="order-customer">{customer.phoneNumber||'Nömrə yoxdur'}</span><span className="order-meta"><span>{customer.deliveryAddress||'Ünvan yoxdur'}</span><b>{customer.username?`@${customer.username}`:''}</b></span></article>):<Empty text="Müştəri yoxdur"/>}</div></section> }

function WorkerPanel({shipments,demo,loading,error,onChange}:{shipments:Shipment[];demo:boolean;loading:boolean;error:string;onChange:(s:Shipment,n:ShipmentStatus)=>void}) { const demoItems:Shipment[]=demo?[{id:'s1',orderNumber:1046,staffId:'w1',staffName:'Elvin Əliyev',status:'Assigned',phoneNumber:'077 329 08 78',deliveryAddress:'Şəfayət Mehdiyev küç. 176',items:[{orderItemId:'i1',productName:'Coca-Cola 1 L',quantity:2}],createdAtUtc:new Date().toISOString()}]:[]; const list=demo?demoItems:shipments; return <main className="miniapp-shell"><header className="app-header"><div className="brand-mark">S</div><div className="brand-copy"><strong>StockMate</strong><span>İşçi paneli</span></div><span className="connection-state">{demo?'Demo':'Telegram'}</span></header>{error&&<div className="toast error">{error}</div>}<section className="screen"><div className="screen-heading"><span>Bugünkü işlər</span><h1>Tapşırıqlarım</h1><p>Yalnız sizə təyin edilən sifarişlər görünür.</p></div><div className="order-list">{list.length?list.map((shipment)=>{const next=nextShipmentStatus(shipment.status);return <article className="detail-card" key={shipment.id}><div className="detail-title"><strong>#{shipment.orderNumber}</strong><span>{shipmentStatus(shipment.status)}</span></div><p>📞 {shipment.phoneNumber}</p><p>📍 {shipment.deliveryAddress}</p><div className="products-box">{shipment.items.map((item)=><div key={item.orderItemId}>{item.quantity} × {item.productName}</div>)}</div>{next&&<button className="primary-action" disabled={loading} onClick={()=>onChange(shipment,next)}>{shipmentStatus(next)}</button>}</article>}):<Empty text="Aktiv tapşırıq yoxdur"/>}</div></section>{loading&&<div className="busy"><span className="spinner"/></div>}</main> }

function Empty({text}:{text:string}) { return <div className="empty-state"><span>📭</span><strong>{text}</strong></div> }
function BottomNav({section,onOpen}:{section:Section;onOpen:(x:Section)=>void}) { return <nav className="bottom-nav"><button className={section==='home'?'active':''} onClick={()=>onOpen('home')}><span>⌂</span>Ana səhifə</button><button className={section==='orders'?'active':''} onClick={()=>onOpen('orders')}><span>▣</span>Sifarişlər</button><button className={section==='products'?'active':''} onClick={()=>onOpen('products')}><span>◇</span>Məhsullar</button><button className={section==='staff'?'active':''} onClick={()=>onOpen('staff')}><span>♙</span>İşçilər</button></nav> }

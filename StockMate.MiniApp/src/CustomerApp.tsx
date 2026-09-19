import { useEffect, useMemo, useRef, useState, type FormEvent } from 'react';
import { api, productImageUrl } from './client';
import type {
  Category,
  CustomerAddress,
  CustomerCart,
  CustomerCatalogProduct,
  CustomerOrder,
  CustomerProfile,
  DeliveryCompany,
  MiniAppIdentity,
  PagedResult,
  Subcategory,
} from './models';
import './customer.css';

type CustomerSection = 'shop' | 'cart' | 'orders' | 'profile';

const emptyCart: CustomerCart = {
  id: '',
  items: [],
  productsTotal: 0,
  expiresAtUtc: '',
};

const demoProducts: CustomerCatalogProduct[] = [
  { id: 'p1', name: 'KENT Blue', code: 'KNT-CQS-BLU', price: 7.5, description: 'Compact QS', categoryId: 'c1', categoryName: 'KENT', subcategoryId: 's1', subcategoryName: 'Compact QS' },
  { id: 'p2', name: 'WOOG Cherry', code: 'WOG-QS-CHE', price: 8.2, description: 'Cherry', categoryId: 'c2', categoryName: 'WOOG', subcategoryId: 's2', subcategoryName: 'Queen Size' },
];

const orderLabels: Record<CustomerOrder['status'], string> = {
  PendingAdminApproval: 'Ожидает подтверждения администратора',
  Confirmed: 'Подтверждён',
  Preparing: 'Готовится',
  Delivering: 'Доставляется',
  Completed: 'Завершён',
  Cancelled: 'Отменён',
};

function money(value?: number) {
  return `${(value ?? 0).toFixed(2)} AZN`;
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat('ru-RU', {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(value));
}

export default function CustomerApp({
  identity,
  demo = false,
}: {
  identity?: MiniAppIdentity | null;
  demo?: boolean;
}) {
  const [section, setSection] = useState<CustomerSection>('shop');
  const [catalog, setCatalog] = useState<PagedResult<CustomerCatalogProduct>>({
    items: demo ? demoProducts : [],
    pageNumber: 1,
    pageSize: 10,
    totalItems: demo ? demoProducts.length : 0,
    totalPages: 1,
  });
  const [categories, setCategories] = useState<Category[]>([]);
  const [subcategories, setSubcategories] = useState<Subcategory[]>([]);
  const [cart, setCart] = useState<CustomerCart>(emptyCart);
  const [profile, setProfile] = useState<CustomerProfile | null>(null);
  const [addresses, setAddresses] = useState<CustomerAddress[]>([]);
  const [companies, setCompanies] = useState<DeliveryCompany[]>([]);
  const [orders, setOrders] = useState<CustomerOrder[]>([]);
  const [categoryId, setCategoryId] = useState('');
  const [subcategoryName, setSubcategoryName] = useState('');
  const [search, setSearch] = useState('');
  const [selectedProduct, setSelectedProduct] = useState<CustomerCatalogProduct | null>(null);
  const [selectedOrder, setSelectedOrder] = useState<CustomerOrder | null>(null);
  const [quantity, setQuantity] = useState(1);
  const [checkoutOpen, setCheckoutOpen] = useState(false);
  const [checkoutPhone, setCheckoutPhone] = useState('');
  const [addressId, setAddressId] = useState('');
  const [companyId, setCompanyId] = useState('');
  const checkoutOperationId = useRef<string | null>(null);
  const [newAddress, setNewAddress] = useState('');
  const [phone, setPhone] = useState('');
  const [loading, setLoading] = useState(!demo);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');

  const visibleSubcategoryNames = useMemo(() => {
    const source = categoryId
      ? subcategories.filter(item => item.categoryId === categoryId)
      : subcategories;
    return [...new Set(source.map(item => item.name))]
      .sort((left, right) => left.localeCompare(right, 'az'));
  }, [categoryId, subcategories]);

  useEffect(() => {
    if (demo) {
      setProfile({ firstName: 'Демо', phoneNumber: '' });
      return;
    }

    void run(async () => {
      const [products, categoryList, subcategoryList, currentCart, currentProfile, addressList, companyList, orderList] = await Promise.all([
        api.getCustomerCatalog(),
        api.getCustomerCategories(),
        api.getCustomerSubcategories(),
        api.getCustomerCart(),
        api.getCustomerProfile(),
        api.getCustomerAddresses(),
        api.getCustomerDeliveryCompanies(),
        api.getMyOrders(),
      ]);
      setCatalog(products);
      setCategories(categoryList);
      setSubcategories(subcategoryList);
      setCart(currentCart);
      setProfile(currentProfile);
      setPhone(currentProfile.phoneNumber ?? '');
      setAddresses(addressList);
      setCompanies(companyList);
      setOrders(orderList);
    });
  }, [demo]);

  async function run(action: () => Promise<void>, successMessage = '') {
    setLoading(true);
    setError('');
    setNotice('');
    try {
      await action();
      if (successMessage) setNotice(successMessage);
      return true;
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Не удалось выполнить операцию.');
      return false;
    } finally {
      setLoading(false);
    }
  }

  async function loadCatalog(pageNumber = 1, nextCategory = categoryId, nextSubcategory = subcategoryName, nextSearch = search) {
    if (demo) return;
    setCatalog(await api.getCustomerCatalog(pageNumber, nextCategory, nextSubcategory, nextSearch));
  }

  async function applyFilters(event?: FormEvent) {
    event?.preventDefault();
    await run(() => loadCatalog(1));
  }

  async function addToCart(product: CustomerCatalogProduct, amount: number) {
    await run(async () => {
      const current = cart.items.find(item => item.productId === product.id)?.quantity ?? 0;
      setCart(demo ? {
        ...cart,
        items: [...cart.items.filter(item => item.productId !== product.id), {
          productId: product.id,
          productName: product.name,
          productCode: product.code,
          unitPrice: product.price,
          quantity: current + amount,
          lineTotal: product.price * (current + amount),
        }],
        productsTotal: cart.productsTotal + product.price * amount,
      } : await api.setCustomerCartItem(product.id, current + amount));
      setSelectedProduct(null);
      setQuantity(1);
    }, 'Товар добавлен в корзину.');
  }

  async function changeCartQuantity(productId: string, nextQuantity: number) {
    await run(async () => {
      if (nextQuantity <= 0) {
        setCart(demo
          ? { ...cart, items: cart.items.filter(item => item.productId !== productId) }
          : await api.removeCustomerCartItem(productId));
      } else {
        setCart(demo
          ? recalculateCart(cart.items.map(item => item.productId === productId
            ? { ...item, quantity: nextQuantity, lineTotal: item.unitPrice * nextQuantity }
            : item), cart)
          : await api.setCustomerCartItem(productId, nextQuantity));
      }
    });
  }

  function beginCheckout() {
    setCheckoutPhone(profile?.phoneNumber ?? phone);
    setAddressId(addresses[0]?.id ?? '');
    setCompanyId(companies[0]?.id ?? '');
    checkoutOperationId.current = crypto.randomUUID();
    setCheckoutOpen(true);
  }

  async function completeCheckout(event: FormEvent) {
    event.preventDefault();
    if (!checkoutPhone.trim() || checkoutPhone.replace(/\D/g, '').length < 9) {
      setError('Введите корректный номер телефона.');
      return;
    }
    if (!addressId || !companyId) {
      setError('Выберите адрес и службу доставки.');
      return;
    }

    const operationId = checkoutOperationId.current ?? crypto.randomUUID();
    checkoutOperationId.current = operationId;

    const completed = await run(async () => {
      if (demo) return;
      if (checkoutPhone.trim() !== profile?.phoneNumber) {
        await api.updateCustomerPhone(checkoutPhone.trim());
      }
      const order = await api.checkoutCustomerOrder(addressId, companyId, operationId);
      setOrders(values => [order, ...values]);
      setProfile(value => value ? { ...value, phoneNumber: checkoutPhone.trim() } : value);
      setPhone(checkoutPhone.trim());
      setCart(await api.getCustomerCart());
    }, 'Ваш заказ принят.');

    if (completed) {
      checkoutOperationId.current = null;
      setCheckoutOpen(false);
      setSection('orders');
    }
  }

  async function savePhone(event: FormEvent) {
    event.preventDefault();
    await run(async () => {
      if (!demo) await api.updateCustomerPhone(phone);
      setProfile(value => value ? { ...value, phoneNumber: phone.trim() } : value);
    }, 'Номер телефона сохранён.');
  }

  async function addAddress(event: FormEvent) {
    event.preventDefault();
    if (!newAddress.trim()) return;
    await run(async () => {
      const created = demo
        ? { id: crypto.randomUUID(), addressLine: newAddress.trim(), isActive: true }
        : await api.createCustomerAddress(newAddress.trim());
      setAddresses(values => [...values, created]);
      setNewAddress('');
    }, 'Адрес добавлен.');
  }

  async function removeAddress(address: CustomerAddress) {
    if (!window.confirm('Удалить этот адрес?')) return;
    await run(async () => {
      if (!demo) await api.deleteCustomerAddress(address.id);
      setAddresses(values => values.filter(value => value.id !== address.id));
    }, 'Адрес удалён.');
  }

  async function editAddress(address: CustomerAddress) {
    const addressLine = window.prompt('Измените адрес:', address.addressLine)?.trim();
    if (!addressLine || addressLine === address.addressLine) return;
    await run(async () => {
      const updated = demo
        ? { ...address, addressLine }
        : await api.updateCustomerAddress(address.id, addressLine);
      setAddresses(values => values.map(value => value.id === address.id ? updated : value));
    }, 'Адрес обновлён.');
  }

  async function openSection(next: CustomerSection) {
    setSection(next);
    if (demo) return;
    await run(async () => {
      if (next === 'cart') setCart(await api.getCustomerCart());
      if (next === 'orders') setOrders(await api.getMyOrders());
      if (next === 'profile') {
        const [currentProfile, addressList, companyList] = await Promise.all([
          api.getCustomerProfile(),
          api.getCustomerAddresses(),
          api.getCustomerDeliveryCompanies(),
        ]);
        setProfile(currentProfile);
        setPhone(currentProfile.phoneNumber ?? '');
        setAddresses(addressList);
        setCompanies(companyList);
      }
    });
  }

  async function cancelOrder(order: CustomerOrder) {
    if (!window.confirm(`Отменить заказ №${order.orderNumber}?`)) return;
    await run(async () => {
      if (!demo) await api.cancelCustomerOrder(order.orderNumber);
      const orderList = demo ? orders.map(value => value.orderNumber === order.orderNumber ? { ...value, status: 'Cancelled' as const } : value) : await api.getMyOrders();
      setOrders(orderList);
      setSelectedOrder(null);
    }, 'Заказ отменён.');
  }

  const displayName = [identity?.firstName ?? profile?.firstName, identity?.lastName ?? profile?.lastName]
    .filter(Boolean)
    .join(' ');

  return <main className="customer-shell">
    <header className="customer-header">
      <div className="customer-brand">S</div>
      <div><strong>StockMate</strong><span>{displayName || 'Магазин'}</span></div>
      <button className="cart-shortcut" onClick={() => void openSection('cart')}>🛒<b>{cart.items.length}</b></button>
    </header>

    {error && <Message text={error} error close={() => setError('')} />}
    {notice && <Message text={notice} close={() => setNotice('')} />}

    {section === 'shop' && <section className="customer-screen">
      <div className="customer-hero"><span>Удобный выбор, быстрый заказ</span><h1>Товары</h1><p>Выберите нужный товар и добавьте его в корзину.</p></div>
      <form className="customer-filters" onSubmit={applyFilters}>
        <input value={search} onChange={event => setSearch(event.target.value)} placeholder="Поиск по товару или коду" />
        <div className="customer-filter-row">
          <select value={categoryId} onChange={event => { setCategoryId(event.target.value); setSubcategoryName(''); }}>
            <option value="">Все марки</option>
            {categories.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}
          </select>
          <select value={subcategoryName} onChange={event => setSubcategoryName(event.target.value)}>
            <option value="">Все размеры</option>
            {visibleSubcategoryNames.map(name => <option key={name} value={name}>{name}</option>)}
          </select>
        </div>
        <div className="customer-filter-actions"><button className="customer-primary" type="submit">Показать</button><button type="button" onClick={() => { setCategoryId(''); setSubcategoryName(''); setSearch(''); void run(() => loadCatalog(1, '', '', '')); }}>Сбросить</button></div>
      </form>
      <div className="customer-product-grid">
        {catalog.items.map(product => <article className="customer-product-card" key={product.id} onClick={() => { setSelectedProduct(product); setQuantity(1); }}>
          {product.imageReference ? <img src={productImageUrl(product.id)} alt={product.name} /> : <div className="customer-product-placeholder">🛍</div>}
          <div><small>{[product.categoryName, product.subcategoryName].filter(Boolean).join(' · ') || product.code}</small><h2>{product.name}</h2><strong>{money(product.price)}</strong></div>
          <button aria-label={`Добавить ${product.name} в корзину`} onClick={event => { event.stopPropagation(); void addToCart(product, 1); }}>+</button>
        </article>)}
        {!catalog.items.length && <CustomerEmpty text="Подходящие товары не найдены" />}
      </div>
      {catalog.totalPages > 1 && <div className="customer-pagination">
        <button disabled={catalog.pageNumber <= 1} onClick={() => run(() => loadCatalog(catalog.pageNumber - 1))}>‹</button>
        <span>{catalog.pageNumber} / {catalog.totalPages}</span>
        <button disabled={catalog.pageNumber >= catalog.totalPages} onClick={() => run(() => loadCatalog(catalog.pageNumber + 1))}>›</button>
      </div>}
    </section>}

    {section === 'cart' && <section className="customer-screen">
      <div className="customer-page-title"><div><span>Ваш заказ</span><h1>Корзина</h1></div>{cart.items.length > 0 && <button onClick={() => run(async () => { if (!demo) await api.clearCustomerCart(); setCart(emptyCart); }, 'Корзина очищена.')}>Очистить</button>}</div>
      <div className="customer-list">
        {cart.items.map(item => <article className="customer-cart-card" key={item.productId}><div><small>{item.productCode}</small><h2>{item.productName}</h2><strong>{money(item.lineTotal)}</strong></div><div className="quantity-control"><button onClick={() => changeCartQuantity(item.productId, item.quantity - 1)}>−</button><b>{item.quantity}</b><button onClick={() => changeCartQuantity(item.productId, item.quantity + 1)}>+</button></div></article>)}
        {!cart.items.length && <CustomerEmpty text="Ваша корзина пуста" />}
      </div>
      {cart.items.length > 0 && <div className="customer-cart-total"><span>Стоимость товаров</span><strong>{money(cart.productsTotal)}</strong><button className="customer-primary" onClick={beginCheckout}>Оформить заказ</button></div>}
    </section>}

    {section === 'orders' && <section className="customer-screen">
      <div className="customer-page-title"><div><span>Отслеживание и история</span><h1>Мои заказы</h1></div></div>
      <div className="customer-list">{orders.map(order => <button className="customer-order-card" key={order.orderNumber} onClick={() => setSelectedOrder(order)}><div><small>{formatDate(order.createdAtUtc)}</small><h2>№{order.orderNumber}</h2></div><div><span>{orderLabels[order.status]}</span><strong>{money(order.totalAmount)}</strong></div></button>)}{!orders.length && <CustomerEmpty text="У вас пока нет заказов" />}</div>
    </section>}

    {section === 'profile' && <section className="customer-screen">
      <div className="customer-page-title"><div><span>Контактные данные</span><h1>Профиль</h1></div></div>
      <form className="customer-panel" onSubmit={savePhone}><label>Номер телефона<input value={phone} onChange={event => setPhone(event.target.value)} placeholder="+994501234567" /></label><button className="customer-primary">Сохранить номер</button></form>
      <form className="customer-panel" onSubmit={addAddress}><h2>Мои адреса</h2><div className="customer-addresses">{addresses.map(address => <div key={address.id}><span>📍 {address.addressLine}</span><span className="customer-address-actions"><button type="button" onClick={() => editAddress(address)}>✎</button><button type="button" onClick={() => removeAddress(address)}>×</button></span></div>)}</div><label>Новый адрес<textarea value={newAddress} onChange={event => setNewAddress(event.target.value)} placeholder="Улица, дом, квартира..." /></label><button className="customer-primary">Добавить адрес</button></form>
    </section>}

    {selectedProduct && <div className="customer-modal" onClick={() => setSelectedProduct(null)}><div className="customer-sheet" onClick={event => event.stopPropagation()}><button className="customer-close" onClick={() => setSelectedProduct(null)}>×</button>{selectedProduct.imageReference ? <img src={productImageUrl(selectedProduct.id)} alt={selectedProduct.name} /> : <div className="customer-detail-placeholder">🛍</div>}<small>{[selectedProduct.categoryName, selectedProduct.subcategoryName].filter(Boolean).join(' · ')}</small><h2>{selectedProduct.name}</h2><p>{selectedProduct.description}</p><strong className="customer-price">{money(selectedProduct.price)}</strong><div className="customer-add-row"><div className="quantity-control"><button onClick={() => setQuantity(value => Math.max(1, value - 1))}>−</button><b>{quantity}</b><button onClick={() => setQuantity(value => Math.min(99, value + 1))}>+</button></div><button className="customer-primary" onClick={() => addToCart(selectedProduct, quantity)}>Добавить в корзину</button></div></div></div>}

    {checkoutOpen && <div className="customer-modal" onClick={() => setCheckoutOpen(false)}><form className="customer-sheet" onClick={event => event.stopPropagation()} onSubmit={completeCheckout}><button type="button" className="customer-close" onClick={() => setCheckoutOpen(false)}>×</button><small>Данные заказа</small><h2>Подтверждение</h2><label>Номер телефона<input value={checkoutPhone} onChange={event => setCheckoutPhone(event.target.value)} placeholder="+994501234567" /></label><label>Адрес доставки<select value={addressId} onChange={event => setAddressId(event.target.value)}><option value="">Выберите адрес</option>{addresses.map(address => <option key={address.id} value={address.id}>{address.addressLine}</option>)}</select></label>{!addresses.length && <p className="customer-warning">Сначала добавьте адрес в разделе «Профиль».</p>}<label>Служба доставки<select value={companyId} onChange={event => setCompanyId(event.target.value)}><option value="">Выберите службу</option>{companies.map(company => <option key={company.id} value={company.id}>{company.name}</option>)}</select></label><div className="customer-confirm-summary"><span>Товары <b>{money(cart.productsTotal)}</b></span><small>Стоимость доставки определит администратор.</small></div><button className="customer-primary" disabled={!addresses.length || !companies.length}>Подтвердить заказ</button></form></div>}

    {selectedOrder && <div className="customer-modal" onClick={() => setSelectedOrder(null)}><div className="customer-sheet" onClick={event => event.stopPropagation()}><button className="customer-close" onClick={() => setSelectedOrder(null)}>×</button><small>{formatDate(selectedOrder.createdAtUtc)}</small><h2>Заказ №{selectedOrder.orderNumber}</h2><span className="customer-order-status">{orderLabels[selectedOrder.status]}</span><div className="customer-order-items">{selectedOrder.items.map(item => <div key={item.productId}><span><b>{item.quantity} ×</b> {item.productName}</span><strong>{money(item.lineTotal)}</strong></div>)}</div><div className="customer-order-meta"><p>📞 {selectedOrder.phoneNumber}</p><p>📍 {selectedOrder.deliveryAddress}</p><p>🚚 {selectedOrder.deliveryCompanyName ?? '—'}</p></div><div className="customer-confirm-summary"><span>Товары <b>{money(selectedOrder.productsTotal)}</b></span>{selectedOrder.deliveryFee !== undefined && <span>Доставка <b>{money(selectedOrder.deliveryFee)}</b></span>}<span>Итого <b>{money(selectedOrder.totalAmount)}</b></span></div>{selectedOrder.status === 'PendingAdminApproval' && <button className="customer-danger" onClick={() => cancelOrder(selectedOrder)}>Отменить заказ</button>}</div></div>}

    <nav className="customer-nav">
      <NavButton active={section === 'shop'} icon="⌂" text="Магазин" onClick={() => void openSection('shop')} />
      <NavButton active={section === 'cart'} icon="🛒" text="Корзина" badge={cart.items.length} onClick={() => void openSection('cart')} />
      <NavButton active={section === 'orders'} icon="▣" text="Заказы" onClick={() => void openSection('orders')} />
      <NavButton active={section === 'profile'} icon="♙" text="Профиль" onClick={() => void openSection('profile')} />
    </nav>
    {loading && <div className="customer-busy"><span /></div>}
  </main>;
}

function recalculateCart(items: CustomerCart['items'], cart: CustomerCart): CustomerCart {
  return { ...cart, items, productsTotal: items.reduce((sum, item) => sum + item.lineTotal, 0) };
}

function NavButton({ active, icon, text, badge, onClick }: { active: boolean; icon: string; text: string; badge?: number; onClick: () => void }) {
  return <button className={active ? 'active' : ''} onClick={onClick}><span>{icon}{badge ? <b>{badge}</b> : null}</span><small>{text}</small></button>;
}

function Message({ text, error = false, close }: { text: string; error?: boolean; close: () => void }) {
  return <div className={`customer-message ${error ? 'error' : ''}`}><span>{text}</span><button onClick={close}>×</button></div>;
}

function CustomerEmpty({ text }: { text: string }) {
  return <div className="customer-empty"><span>🛍</span><strong>{text}</strong></div>;
}

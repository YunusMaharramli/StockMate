import type {
  Category,
  CustomerCart,
  CustomerCatalogProduct,
  MiniAppIdentity,
  Subcategory,
} from './models';

const databaseName = 'stockmate-miniapp';
const databaseVersion = 1;
const identityStore = 'customer-identities';
const cartStore = 'customer-carts';
const catalogStore = 'customer-catalogs';
const identityLifetimeMs = 24 * 60 * 60 * 1000;

export interface CachedCustomerCart {
  telegramUserId: number;
  cart: CustomerCart;
  updatedAtUtc: string;
  isDirty: boolean;
}

export interface CachedCustomerCatalog {
  telegramUserId: number;
  products: CustomerCatalogProduct[];
  categories: Category[];
  subcategories: Subcategory[];
  updatedAtUtc: string;
}

interface CachedCustomerIdentity {
  telegramUserId: number;
  identity: MiniAppIdentity;
  expiresAtUtc: string;
}

function openDatabase(): Promise<IDBDatabase> {
  return new Promise((resolve, reject) => {
    const request = indexedDB.open(databaseName, databaseVersion);

    request.onupgradeneeded = () => {
      const database = request.result;
      for (const storeName of [identityStore, cartStore, catalogStore]) {
        if (!database.objectStoreNames.contains(storeName)) {
          database.createObjectStore(storeName, { keyPath: 'telegramUserId' });
        }
      }
    };

    request.onsuccess = () => resolve(request.result);
    request.onerror = () => reject(
      request.error ?? new Error('Не удалось открыть локальное хранилище.'),
    );
    request.onblocked = () => reject(
      new Error('Обновление локального хранилища заблокировано другой страницей.'),
    );
  });
}

function transactionCompleted(transaction: IDBTransaction): Promise<void> {
  return new Promise((resolve, reject) => {
    transaction.oncomplete = () => resolve();
    transaction.onerror = () => reject(
      transaction.error ?? new Error('Операция локального хранилища завершилась ошибкой.'),
    );
    transaction.onabort = () => reject(
      transaction.error ?? new Error('Операция локального хранилища была прервана.'),
    );
  });
}

async function getValue<T>(storeName: string, key: number): Promise<T | null> {
  const database = await openDatabase();

  try {
    const transaction = database.transaction(storeName, 'readonly');
    const completed = transactionCompleted(transaction);
    const request = transaction.objectStore(storeName).get(key);
    const result = await new Promise<T | undefined>((resolve, reject) => {
      request.onsuccess = () => resolve(request.result as T | undefined);
      request.onerror = () => reject(
        request.error ?? new Error('Не удалось прочитать локальные данные.'),
      );
    });

    await completed;
    return result ?? null;
  } finally {
    database.close();
  }
}

async function putValue(storeName: string, value: object): Promise<void> {
  const database = await openDatabase();

  try {
    const transaction = database.transaction(storeName, 'readwrite');
    const completed = transactionCompleted(transaction);
    transaction.objectStore(storeName).put(value);
    await completed;
  } finally {
    database.close();
  }
}

async function deleteValue(storeName: string, key: number): Promise<void> {
  const database = await openDatabase();

  try {
    const transaction = database.transaction(storeName, 'readwrite');
    const completed = transactionCompleted(transaction);
    transaction.objectStore(storeName).delete(key);
    await completed;
  } finally {
    database.close();
  }
}

export function telegramUserIdFromInitData(): number | null {
  const rawUser = new URLSearchParams(window.Telegram?.WebApp.initData ?? '')
    .get('user');

  if (!rawUser) return null;

  try {
    const value = JSON.parse(rawUser) as { id?: unknown };
    return typeof value.id === 'number' ? value.id : null;
  } catch {
    return null;
  }
}

export function isNetworkFailure(reason: unknown): boolean {
  if (reason instanceof TypeError) return true;
  if (!(reason instanceof Error)) return false;

  const message = reason.message.toLocaleLowerCase();
  return message.includes('failed to fetch')
    || message.includes('networkerror')
    || message.includes('network request failed');
}

export async function saveCustomerIdentity(
  identity: MiniAppIdentity,
): Promise<void> {
  if (identity.role !== 'Customer') return;

  const value: CachedCustomerIdentity = {
    telegramUserId: identity.telegramUserId,
    identity,
    expiresAtUtc: new Date(Date.now() + identityLifetimeMs).toISOString(),
  };

  await putValue(identityStore, value);
}

export async function getCustomerIdentity(
  telegramUserId: number,
): Promise<MiniAppIdentity | null> {
  const cached = await getValue<CachedCustomerIdentity>(
    identityStore,
    telegramUserId,
  );

  if (!cached) return null;
  if (new Date(cached.expiresAtUtc).getTime() > Date.now()) {
    return cached.identity.role === 'Customer' ? cached.identity : null;
  }

  await deleteValue(identityStore, telegramUserId);
  return null;
}

export async function deleteCustomerIdentity(
  telegramUserId: number,
): Promise<void> {
  await deleteValue(identityStore, telegramUserId);
}

export function getCachedCustomerCart(
  telegramUserId: number,
): Promise<CachedCustomerCart | null> {
  return getValue<CachedCustomerCart>(cartStore, telegramUserId);
}

export function saveCachedCustomerCart(
  telegramUserId: number,
  cart: CustomerCart,
  isDirty: boolean,
): Promise<void> {
  const value: CachedCustomerCart = {
    telegramUserId,
    cart,
    isDirty,
    updatedAtUtc: new Date().toISOString(),
  };

  return putValue(cartStore, value);
}

export function getCachedCustomerCatalog(
  telegramUserId: number,
): Promise<CachedCustomerCatalog | null> {
  return getValue<CachedCustomerCatalog>(catalogStore, telegramUserId);
}

export async function saveCachedCustomerCatalog(
  telegramUserId: number,
  products: CustomerCatalogProduct[],
  categories: Category[],
  subcategories: Subcategory[],
): Promise<void> {
  const current = await getCachedCustomerCatalog(telegramUserId);
  const mergedProducts = new Map(
    (current?.products ?? []).map(product => [product.id, product]),
  );

  for (const product of products) {
    mergedProducts.set(product.id, product);
  }

  const value: CachedCustomerCatalog = {
    telegramUserId,
    products: [...mergedProducts.values()],
    categories,
    subcategories,
    updatedAtUtc: new Date().toISOString(),
  };

  await putValue(catalogStore, value);
}

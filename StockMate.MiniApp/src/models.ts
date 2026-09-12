export type StaffRole = 'Admin' | 'Worker';
export type OrderStatus = 'PendingAdminApproval' | 'Confirmed' | 'Preparing' | 'Delivering' | 'Completed' | 'Cancelled';
export type ShipmentStatus = 'Assigned' | 'Preparing' | 'Ready' | 'OutForDelivery' | 'Delivered' | 'Cancelled';
export type DeliveryMode = 'Undecided' | 'AllTogether' | 'PartialAllowed';
export type FulfillmentStatus = 'Waiting' | 'PartiallyDelivered' | 'FullyDelivered' | 'ClosedWithShortage';
export type SpecialOrderStatus = 'NotRequired' | 'Pending' | 'OrderedFromSupplier' | 'Arrived' | 'Completed' | 'Cancelled';
export type StockAction = 'add' | 'remove' | 'set';
export type MiniAppUserRole = 'Admin' | 'Worker' | 'Customer';

export interface MiniAppIdentity {
  telegramUserId: number;
  role: MiniAppUserRole;
  staffId?: string;
  customerId?: string;
  firstName: string;
  lastName?: string;
  username?: string;
}

export interface Staff {
  id: string;
  telegramUserId: number;
  firstName: string;
  lastName?: string;
  username?: string;
  role: StaffRole;
  isActive: boolean;
}

export interface OrderItem {
  id: string;
  productId: string;
  productName: string;
  productCode: string;
  unitPrice: number;
  fromStockQuantity: number;
  specialOrderQuantity: number;
  deliveredQuantity: number;
  cancelledQuantity: number;
  remainingQuantity: number;
  readyQuantity: number;
  totalQuantity: number;
  lineTotal: number;
  specialOrderStatus: SpecialOrderStatus;
}

export interface Order {
  id: string;
  orderNumber: number;
  customerId: string;
  customerAddressId?: string;
  phoneNumber: string;
  deliveryAddress: string;
  latitude?: number;
  longitude?: number;
  deliveryCompanyId?: string;
  deliveryCompanyName?: string;
  status: OrderStatus;
  deliveryMode: DeliveryMode;
  items: OrderItem[];
  productsTotal: number;
  deliveryFee?: number;
  totalAmount: number;
  containsSpecialOrder: boolean;
  fulfillmentStatus: FulfillmentStatus;
  remainingQuantity: number;
  readyQuantity: number;
  adminNote?: string;
  cancellationReason?: string;
  createdAtUtc: string;
  confirmedAtUtc?: string;
  completedAtUtc?: string;
  cancelledAtUtc?: string;
}

export interface Product {
  id: string;
  name: string;
  code: string;
  price: number;
  stockQuantity: number;
  description: string;
  imageReference?: string;
  isActive: boolean;
  isArchived: boolean;
  categoryId?: string;
  categoryName?: string;
  subcategoryId?: string;
  subcategoryName?: string;
  minimumStockQuantity: number;
  isLowStock: boolean;
  isCatalogVisible: boolean;
}

export interface Category {
  id: string;
  name: string;
  isActive: boolean;
  isArchived: boolean;
  sortOrder: number;
  subcategoryCount: number;
  productCount: number;
}

export interface Subcategory {
  id: string;
  categoryId: string;
  categoryName: string;
  name: string;
  isActive: boolean;
  isArchived: boolean;
  sortOrder: number;
  productCount: number;
}

export interface ProductInput {
  name: string;
  code: string;
  price: number;
  initialStockQuantity: number;
  description: string;
  imageReference?: string;
  categoryId: string;
  subcategoryId?: string;
  minimumStockQuantity: number;
}

export interface StockMovement {
  id: string;
  type: 'InitialStock' | 'StockAdded' | 'StockRemoved' | 'StockCorrected' | 'OrderConfirmed' | 'OrderCancelled';
  previousQuantity: number;
  newQuantity: number;
  quantityChange: number;
  reason: string;
  orderId?: string;
  createdAtUtc: string;
}

export interface PagedResult<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

export interface CustomerCatalogProduct {
  id: string;
  name: string;
  code: string;
  price: number;
  description: string;
  imageReference?: string;
  categoryId?: string;
  categoryName?: string;
  subcategoryId?: string;
  subcategoryName?: string;
}

export interface CustomerCartItem {
  productId: string;
  productName: string;
  productCode: string;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
}

export interface CustomerCart {
  id: string;
  items: CustomerCartItem[];
  productsTotal: number;
  expiresAtUtc: string;
}

export interface CustomerProfile {
  firstName: string;
  lastName?: string;
  username?: string;
  phoneNumber?: string;
}

export interface CustomerAddress {
  id: string;
  addressLine: string;
  latitude?: number;
  longitude?: number;
  isActive: boolean;
}

export interface CustomerOrderItem {
  productId: string;
  productName: string;
  productCode: string;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
}

export interface CustomerOrder {
  orderNumber: number;
  phoneNumber: string;
  deliveryAddress: string;
  deliveryCompanyName?: string;
  status: OrderStatus;
  items: CustomerOrderItem[];
  productsTotal: number;
  deliveryFee?: number;
  totalAmount: number;
  cancellationReason?: string;
  createdAtUtc: string;
  confirmedAtUtc?: string;
  completedAtUtc?: string;
  cancelledAtUtc?: string;
}

export interface Customer {
  id: string;
  telegramUserId: number;
  firstName: string;
  lastName?: string;
  username?: string;
  phoneNumber?: string;
  deliveryAddress?: string;
  latitude?: number;
  longitude?: number;
  productPageSize: number;
  isBlocked: boolean;
  blockedAtUtc?: string;
}

export interface Assignment {
  id: string;
  orderNumber: number;
  staffId: string;
  staffName: string;
  assignedAtUtc: string;
}

export interface ShipmentItem {
  orderItemId: string;
  productName: string;
  quantity: number;
}

export interface Shipment {
  id: string;
  orderNumber: number;
  staffId: string;
  staffName: string;
  status: ShipmentStatus;
  phoneNumber: string;
  deliveryAddress: string;
  latitude?: number;
  longitude?: number;
  items: ShipmentItem[];
  createdAtUtc: string;
  deliveredAtUtc?: string;
}

export interface MissingProduct {
  productId: string;
  productName: string;
  productCode: string;
  missingQuantity: number;
  affectedOrderCount: number;
}

export interface MissingProductOrder {
  orderNumber: number;
  customerId: string;
  customerName: string;
  missingQuantity: number;
  status: SpecialOrderStatus;
}

export interface StockPurchaseItem {
  id: string;
  productId: string;
  productName: string;
  productCode: string;
  quantity: number;
  unitCost: number;
  lineTotal: number;
}

export interface CreateStockPurchaseItem {
  productId: string;
  quantity: number;
  unitCost: number;
}

export interface StockPurchase {
  id: string;
  purchasedAtUtc: string;
  note?: string;
  items: StockPurchaseItem[];
  totalAmount: number;
  createdAtUtc: string;
}

export interface DeliveryCompany {
  id: string;
  name: string;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc?: string;
}

export interface ProductTurnover {
  productId: string;
  productName: string;
  productCode: string;
  purchasedQuantity: number;
  averagePurchaseUnitCost: number;
  purchaseAmount: number;
  soldQuantity: number;
  averageSaleUnitPrice: number;
  salesAmount: number;
}

export interface InventoryTurnoverReport {
  fromUtc: string;
  toUtc: string;
  deliveryCompanyId?: string;
  deliveryCompanyName?: string;
  products: ProductTurnover[];
  totalPurchasedQuantity: number;
  totalPurchaseAmount: number;
  totalSoldQuantity: number;
  totalSalesAmount: number;
}

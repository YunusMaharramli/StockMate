export type StaffRole = 'Admin' | 'Worker';
export type OrderStatus = 'PendingAdminApproval' | 'Confirmed' | 'Preparing' | 'Delivering' | 'Completed' | 'Cancelled';
export type ShipmentStatus = 'Assigned' | 'Preparing' | 'Ready' | 'OutForDelivery' | 'Delivered' | 'Cancelled';

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
  productName: string;
  unitPrice: number;
  totalQuantity: number;
  readyQuantity: number;
  deliveredQuantity: number;
  cancelledQuantity: number;
  remainingQuantity: number;
  lineTotal: number;
}

export interface Order {
  id: string;
  orderNumber: number;
  customerId: string;
  phoneNumber: string;
  deliveryAddress: string;
  latitude?: number;
  longitude?: number;
  status: OrderStatus;
  deliveryMode: 'Undecided' | 'AllTogether' | 'PartialAllowed';
  items: OrderItem[];
  productsTotal: number;
  deliveryFee?: number;
  totalAmount: number;
  containsSpecialOrder: boolean;
  fulfillmentStatus: 'Waiting' | 'PartiallyDelivered' | 'FullyDelivered' | 'ClosedWithShortage';
  remainingQuantity: number;
  readyQuantity: number;
  createdAtUtc: string;
  completedAtUtc?: string;
  cancellationReason?: string;
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
}

export interface PagedResult<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

export interface Customer {
  id: string;
  telegramUserId: number;
  firstName: string;
  lastName?: string;
  username?: string;
  phoneNumber?: string;
  deliveryAddress?: string;
}

export interface Assignment {
  id: string;
  orderNumber: number;
  staffId: string;
  staffName: string;
  assignedAtUtc: string;
}

export interface ShipmentItem { orderItemId: string; productName: string; quantity: number }
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

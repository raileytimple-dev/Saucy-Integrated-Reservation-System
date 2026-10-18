# Original Saucy -> MVC feature map

| Original area | MVC destination | Kept behavior |
|---|---|---|
| HOME.aspx | Home/Index | branding, featured dishes, navigation |
| FAMOUS OFFERINGS / Pasta / Sides pages | Menu/Index | menu categories, item cards, ordering |
| LOG IN / SIGN UP | Account/Login, Account/Register | customer accounts |
| RESERVATION.aspx | Reservations/Create + Reservations/Index | scheduling, guest count, table assignment, status |
| TRACK ORDERS.aspx | Orders/Track | active order status/progress |
| ORDER HISTORY.aspx | Orders/History | previous orders, totals, status |
| Cart / checkout logic | Cart/Index | quantities, totals, checkout |
| Payment flow | Payments/Pay | payment method/reference, transaction creation |
| Homepage-ReportsAnalytics.aspx | Admin/Index | dashboard summary |
| Reservation-ManageReservation* | Admin/Reservations | filters + status actions |
| Reservation-AddTable / TrackTableAvailability | Admin/DiningTables | add/edit/status/delete tables |
| Orders-ManageOrders* / OrderHistory* | Admin/Orders | order filtering and workflow status |
| Menu-UpdateMenu.aspx | Admin/Menu | add/edit/status menu items |
| Management-ViewTrackPayments* | Admin/Payments | payment tracking filters |
| Management-RefundTransaction* | Admin/Refunds | approve/decline refund requests |
| Management-ViewTransactionHistory* | Admin/Transactions | search/today/all, add/delete records |
| Accounts-* | Admin/Accounts | role filters, active/inactive, create staff/admin |
| Staff-ManageOrders* | Staff/Orders | staff order workflow |
| Staff-OrderHistory* | Staff/History | completed/cancelled order history |

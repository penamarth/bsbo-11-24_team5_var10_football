**Операция**: getTicketInfo(scannedData: ScannedTicketData)

**Ссылки:** Прецеденты: Возврат билетов и абонементов

**Предусловия:**
- Получены данные сканирования `scannedData`.
- Локальная БД доступна.

**Постусловия:**
- Найден экземпляр `t` класса `Ticket` или `Subscription`, соответствующий `scannedData` (поиск объекта).
- Прочитаны атрибуты `t.used`, `t.paymentType`, `t.eventId`, `t.status` (чтение данных).
- Выполнены проверки `checkExistence`, `checkUsed`, `checkRefundEligibility` (проверка данных).
- Сформирован объект `ticketInfo` с результатом проверки.
- `ticketInfo` передан в `RefundController`.
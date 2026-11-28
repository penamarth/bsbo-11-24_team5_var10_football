**Операция**: startRefund()

**Ссылки:** Прецеденты: Возврат билетов и абонементов

**Предусловия:**
- КСО находиться на начальной странице
- КСО исправно
- Пользователь нажал на кнопку "Вернуть средства за пропуск"

**Постусловия:**
- КСО переходит на страницу возврата
- FTS вызвал `requestScan()` и получил `passId`.
- `PassRepository.checkRefundEligibility(passId)` разрешил возврат, `pass` найден через `PassRepository.getPassById(passId)`.
- FTS вызвал `showRefundDetails(pass)` и `refundToCard(pass)` или `refundWithCash(pass)`.
- FTS вызвал `PassRepository.refund(passId)`.
- FTS выбрал `RefundStrategy` по типу пропуска и вызвал `releaseSeats(pass, eventRepo)`.
- FTS вызвал `giveReceipt(pass)`.
- Каждая вызванная FTS функция выведена в отдельной панели логов.

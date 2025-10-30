# Luồng hoạt động - Gói BlindBox theo tuần

## 🔵 LUỒNG USER

### 1. Đăng ký gói subscription
**API:** `POST /api/WeeklyBlindBoxSubscription`

**Request:**
```json
{
  "boxTypeId": "guid-blindbox-id",
  "startDate": "2024-01-01",
  "durationWeeks": 4,
  "paymentMethod": "PayOS",
  "address": "123 Đường ABC",
  "deliveryTo": "Nguyễn Văn A",
  "phoneNumber": "0901234567",
  "firstDeliveryDay": "Monday",
  "secondDeliveryDay": "Thursday",
  "allergyNote": "Dị ứng đậu",
  "preferenceNote": "Ưu tiên đồ chay"
}
```

**Kết quả:**
- ✅ Tạo 1 Order để thanh toán (giá ưu đãi)
- ✅ Tạo Subscription record
- ✅ Tạo Delivery Schedules cho tất cả các tuần
- ✅ Trả về thông tin subscription với lịch giao hàng

---

### 2. Thanh toán Order
- User thanh toán Order đã tạo (qua PayOS/VNPay)
- Khi thanh toán thành công → PaymentStatus = Paid

---

### 3. Xem thông tin gói subscription
**API:** `GET /api/WeeklyBlindBoxSubscription/my-subscriptions`

**Response:** Danh sách tất cả subscriptions của user:
- Thông tin gói: Ngày bắt đầu/kết thúc, số tuần, số tuần còn lại
- Giá cả: Giá/tuần, tổng giá, giá/box, tiết kiệm so với mua lẻ
- Trạng thái: Active/Paused/Expired/Cancelled
- Lịch giao hàng: Tất cả delivery schedules

---

### 4. Xem chi tiết 1 subscription
**API:** `GET /api/WeeklyBlindBoxSubscription/{subscriptionId}`

**Response:** Thông tin chi tiết bao gồm:
- Thông tin user, box type
- Ngày bắt đầu/kết thúc, chu kỳ thanh toán
- Toàn bộ lịch giao hàng với trạng thái giao hàng

---

### 5. Xem lịch giao hàng cụ thể
**API:** `GET /api/WeeklyBlindBoxSubscription/{subscriptionId}/delivery-schedules`

**Response:** Danh sách từng tuần:
- **WeekStartDate/WeekEndDate**: Tuần này
- **FirstDeliveryDate**: Ngày giao lần 1 (Thứ 2)
  - `IsFirstDelivered`: Đã giao chưa
  - `FirstDeliveredAt`: Thời gian thực tế giao
- **SecondDeliveryDate**: Ngày giao lần 2 (Thứ 5)
  - `IsSecondDelivered`: Đã giao chưa
  - `SecondDeliveredAt`: Thời gian thực tế giao
- **DeliveryCount**: Số lần đã giao trong tuần (0, 1, hoặc 2)
- **IsPaused**: Có bị hoãn không

**Ví dụ:**
```json
{
  "id": "...",
  "weekStartDate": "2024-01-01",
  "weekEndDate": "2024-01-07",
  "firstDeliveryDate": "2024-01-01",
  "isFirstDelivered": true,
  "firstDeliveredAt": "2024-01-01 10:00",
  "secondDeliveryDate": "2024-01-04",
  "isSecondDelivered": false,
  "deliveryCount": 1  // Đã giao 1/2 lần
}
```

---

### 6. Gia hạn gói
**API:** `POST /api/WeeklyBlindBoxSubscription/renew`

**Request:**
```json
{
  "subscriptionId": "guid-subscription-id",
  "additionalWeeks": 4
}
```

**Kết quả:**
- ✅ Tạo 1 Order mới để thanh toán phần gia hạn (giá ưu đãi)
- ✅ Cập nhật `EndDate`, `DurationWeeks`, `TotalPrice`
- ✅ Tạo thêm Delivery Schedules cho các tuần mới

---

### 7. Yêu cầu hoãn giao hàng (tuần này)
User liên hệ admin → Admin sẽ hoãn qua API

---

## 🔴 LUỒNG ADMIN

### 1. Xem tất cả subscriptions
**API:** `GET /api/WeeklyBlindBoxSubscription/admin/all`

**Response:** Danh sách tất cả subscriptions của tất cả users

---

### 2. Xem các delivery sắp đến (hôm nay hoặc sắp tới)
**API:** `GET /api/WeeklyBlindBoxSubscription/admin/pending-deliveries`

**Response:** Danh sách các delivery chưa giao:
- Subscription info (user, box type, địa chỉ)
- Delivery dates
- Trạng thái đã giao chưa
- Filter: Chỉ lấy những delivery chưa giao và **chưa bị hoãn**

**Ví dụ response:**
```json
{
  "data": [
    {
      "id": "schedule-id",
      "subscriptionId": "...",
      "weekStartDate": "2024-01-01",
      "firstDeliveryDate": "2024-01-01",
      "isFirstDelivered": false,  // Chưa giao
      "secondDeliveryDate": "2024-01-04",
      "isSecondDelivered": false, // Chưa giao
      "deliveryCount": 0,
      "subscriptionInfo": {
        "userId": "...",
        "userName": "Nguyễn Văn A",
        "userEmail": "user@example.com",
        "address": "123 Đường ABC",
        "phoneNumber": "0901234567"
      }
    }
  ]
}
```

**Mục đích:** Admin xem danh sách để chuẩn bị giao hàng hôm nay

---

### 3. Hoãn giao hàng cho 1 tuần (khi user yêu cầu)
**API:** `POST /api/WeeklyBlindBoxSubscription/admin/pause-week`

**Request:**
```json
{
  "subscriptionId": "guid-subscription-id",
  "weekStartDate": "2024-01-01",  // Thứ 2 đầu tuần cần hoãn
  "reason": "User yêu cầu hoãn tuần này"
}
```

**Kết quả:**
- ✅ Set `IsPaused = true` cho delivery schedule của tuần đó
- ✅ Không giao hàng tuần này
- ✅ Lưu `PauseReason`

---

### 4. Xem các delivery đã bị hoãn (để giao bù lại)
**API:** `GET /api/WeeklyBlindBoxSubscription/admin/paused-deliveries`

**Response:** Danh sách các delivery đã bị hoãn và **chưa được giao bù lại**:
- Filter: `IsPaused = true` và chưa giao đủ (`DeliveryCount < 2`)
- Bao gồm thông tin subscription (user, địa chỉ) để admin biết cần giao cho ai

**Mục đích:** Admin xem để biết còn bao nhiêu delivery chưa giao bù lại

---

### 5. Bỏ hoãn và schedule lại delivery (giao hàng bù lại)
**API:** `POST /api/WeeklyBlindBoxSubscription/admin/resume-delivery`

**Request:**
```json
{
  "scheduleId": "guid-schedule-id",
  "newFirstDeliveryDate": "2024-01-08",  // Optional - ngày giao mới cho lần 1
  "newSecondDeliveryDate": "2024-01-11" // Optional - ngày giao mới cho lần 2
}
```

**Kết quả:**
- ✅ Set `IsPaused = false`
- ✅ Xóa `PauseReason`
- ✅ Cập nhật ngày giao hàng mới (nếu có)
- ✅ Delivery này sẽ xuất hiện trong `pending-deliveries` khi đến ngày

---

### 6. Đánh dấu đã giao hàng
**API:** `POST /api/WeeklyBlindBoxSubscription/admin/mark-delivery`

**Request:**
```json
{
  "scheduleId": "guid-schedule-id",
  "deliveryNumber": 1,  // 1 = lần giao đầu tiên, 2 = lần giao thứ hai
  "deliveredAt": "2024-01-01T10:00:00"  // Optional, nếu null thì dùng thời gian hiện tại
}
```

**Kết quả:**
- ✅ Set `IsFirstDelivered = true` hoặc `IsSecondDelivered = true`
- ✅ Lưu `FirstDeliveredAt` hoặc `SecondDeliveredAt`
- ✅ `DeliveryCount` tự động cập nhật (0→1 hoặc 1→2)

**Ví dụ:**
- Giao lần 1: `DeliveryNumber = 1` → `IsFirstDelivered = true`, `DeliveryCount = 1`
- Giao lần 2: `DeliveryNumber = 2` → `IsSecondDelivered = true`, `DeliveryCount = 2`

---

## 📊 Tracking số lần giao hàng

**WeeklyDeliverySchedule** tự động tracking:
- `DeliveryCount = 0`: Chưa giao lần nào
- `DeliveryCount = 1`: Đã giao 1/2 lần (chỉ giao lần 1 HOẶC lần 2)
- `DeliveryCount = 2`: Đã giao đủ 2/2 lần

**Công thức:**
```csharp
DeliveryCount = (IsFirstDelivered ? 1 : 0) + (IsSecondDelivered ? 1 : 0)
```

---

## 🔄 QUY TRÌNH HÀNG NGÀY CỦA ADMIN

### Buổi sáng (9h - 10h):
1. **Xem delivery hôm nay:**
   - Gọi API `GET /api/WeeklyBlindBoxSubscription/admin/pending-deliveries`
   - Xem danh sách các delivery cần giao hôm nay
   - Chuẩn bị hàng theo danh sách

2. **Kiểm tra delivery bị hoãn (nếu cần giao bù lại):**
   - Gọi API `GET /api/WeeklyBlindBoxSubscription/admin/paused-deliveries`
   - Xem danh sách delivery đã bị hoãn và chưa giao bù
   - Quyết định khi nào giao bù lại

### Khi user yêu cầu hoãn:
1. User liên hệ admin
2. Admin gọi API `POST /api/WeeklyBlindBoxSubscription/admin/pause-week`
3. Delivery được đánh dấu `IsPaused = true`

### Khi giao hàng bù lại (resume):
1. Admin quyết định giao bù lại
2. Gọi API `POST /api/WeeklyBlindBoxSubscription/admin/resume-delivery`
   - Có thể set ngày giao mới (newFirstDeliveryDate, newSecondDeliveryDate)
   - Hoặc để null để giữ nguyên ngày cũ
3. Delivery sẽ xuất hiện trong `pending-deliveries` khi đến ngày

### Khi giao hàng:
- Sau khi giao xong → Gọi API `POST /api/WeeklyBlindBoxSubscription/admin/mark-delivery`
- Đánh dấu `deliveryNumber = 1` hoặc `2` tương ứng
- `DeliveryCount` tự động cập nhật

### Kiểm tra:
- Admin có thể xem lại lịch giao hàng để biết:
  - Tuần này đã giao cho user này 1 hay 2 lần (`DeliveryCount`)
  - Lần nào đã giao, lần nào chưa
  - Có delivery nào bị hoãn chưa giao bù lại không

---

## 📝 LƯU Ý

1. **Order chỉ tạo 2 lần:**
   - Khi đăng ký gói
   - Khi gia hạn gói

2. **Không tự động tạo orders khi đến ngày giao:**
   - Lịch giao hàng chỉ để tracking
   - Admin xem lịch và chuẩn bị giao hàng thủ công

3. **Tracking tự động:**
   - `DeliveryCount` tự động tính từ `IsFirstDelivered` và `IsSecondDelivered`
   - Admin chỉ cần đánh dấu khi giao hàng


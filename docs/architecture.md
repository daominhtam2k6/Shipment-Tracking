# MEP - Mô hình trao đổi thông điệp

| Mũi tên trong sơ đồ đề tài | MEP | Chapter |
| --- | --- | --- |
| Operator Client → Tracking API | Yêu cầu–phản hồi (REST) | 1, 2 |
| Customer Client → Tracking API (tra cứu) | Yêu cầu–phản hồi (REST) | 2 |
| Tracking API → Database | Truy cập dữ liệu (SQL), không phải MEP | thiếu trong giáo trình |
| Tracking API → RabbitMQ | Publish sự kiện | 6, 7 |
| RabbitMQ → Notification Service | Pub/sub, mỗi subscriber một queue riêng | 6, 7 |
| Notification Service → Customer Client | Đẩy dữ liệu (SignalR) | 3, 7 |
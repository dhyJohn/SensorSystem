# SensorSystem

## 📝 개요

**SensorSystem**은 C# 기반 TCP 서버와 C++ 기반 TCP 클라이언트로 구성된 센서 데이터 통신 시스템입니다.  
이 프로젝트는 네트워크 프로그래밍 및 IoT 시뮬레이션을 위한 테스트베드로 설계되었습니다.

---

## 📂 프로젝트 구성

- `SensorSystem.sln`  
  Visual Studio 솔루션 파일
  
- `SensorTCPServer`  
  **C# TCP 서버 애플리케이션**
  - 클라이언트로부터 센서 데이터를 수신
  - 로그인후 대쉬보드로 이동하여 5초마다 실시간으로 클라이언트에서 임의로 만든 데이터를 갱신하여 보여줌
  - C# 서버 애플리케이션은 HttpClient를 활용하여, JSON 형태의 센서 데이터를 Django REST API 엔드포인트에 비동기 POST 방식으로 전송하며, OAuth2 Bearer Token 인증을 통해 요청의 신뢰성을 보장합니다. 응답은 비동기로 수신되어 로깅 및 예외 처리 루틴에 의해 처리됩니다.
  - C# 서버 애플리케이션은 Telegram Bot API와 연동하여, 지정된 채팅 ID로 실시간 경보 메시지를 비동기 전송합니다.
    HTTP 클라이언트를 활용해 application/x-www-form-urlencoded 형식으로 요청 본문을 구성하며, 전송 결과는 비동기적으로 수신해 로깅 처리됩니다.
- `SensorTCPClient`  
  **C++ TCP 클라이언트 애플리케이션**
  - 임의 센서 데이터를 생성해 서버에 송신


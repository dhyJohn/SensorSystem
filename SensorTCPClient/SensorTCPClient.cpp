#define NOMINMAX
#include <iostream>
#include <thread>
#include <chrono>
#include <map>
#include <winsock2.h>
#include <ws2tcpip.h>
#include <nlohmann/json.hpp>
#include <random>
#include <algorithm>
#pragma comment(lib, "ws2_32.lib")

using json = nlohmann::json;

// 전역 변수
std::map<int, float> volumes = { {1, 500}, {2, 300}, {3, 600}, {4, 400}, {5, 350}, {6, 450} };
std::map<int, float> waters = { {1, 5}, {2, 3}, {3, 4}, {4, 2}, {5, 1.5}, {6, 2.5} };
std::map<int, float> temps = { {1, 25}, {2, 24}, {3, 26}, {4, 23}, {5, 22}, {6, 27} };
std::string fuelNames[6] = { u8"경유", u8"휘발유", u8"등유", u8"중유", u8"항공유", u8"LPG" };

// 난수 생성
float random_variation(float base, float range) {
    static std::default_random_engine e(static_cast<unsigned>(time(nullptr)));
    std::uniform_real_distribution<float> dist(-range, range);
    return base + dist(e);
}

// 메인 전송 루프
void sendAllTanksLoop() {
    WSADATA wsaData;
    WSAStartup(MAKEWORD(2, 2), &wsaData);

    while (true) {
        json tanks = json::array();
        for (int i = 1; i <= 6; ++i) {
            volumes[i] = std::max(0.0f, volumes[i] - random_variation(1.0f, 0.3f));
            waters[i] = std::max(0.0f, waters[i] - random_variation(0.1f, 0.05f));
            temps[i] = random_variation(temps[i], 0.5f);
            tanks.push_back({
                {"tank", i},
                {"name", fuelNames[i - 1]},
                {"volume", volumes[i]},
                {"capacity", 1000.0},
                {"temp", temps[i]},
                {"water", waters[i]}
                });
        }
        std::string jsonStr = tanks.dump() + "\n";
        SOCKET sock = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
        sockaddr_in serverAddr{};
        serverAddr.sin_family = AF_INET;
        serverAddr.sin_port = htons(9000);
        inet_pton(AF_INET, "127.0.0.1", &serverAddr.sin_addr);

        if (connect(sock, (SOCKADDR*)&serverAddr, sizeof(serverAddr)) == 0) {
            send(sock, jsonStr.c_str(), static_cast<int>(jsonStr.length()), 0);
            std::cout << tanks.dump(4) << std::endl;
        }
        closesocket(sock);
        std::this_thread::sleep_for(std::chrono::seconds(5)); // 고정 주기
    }

    WSACleanup();
}

int main() {
    SetConsoleOutputCP(CP_UTF8);
    sendAllTanksLoop(); // 단일 루프 실행
    return 0;
}

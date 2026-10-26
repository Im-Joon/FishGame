using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BattleArena
{
    /// <summary>
    /// TCP 패킷 송수신 담당
    ///
    /// 패킷 형식 : [ 본문길이(4바이트) ][ JSON 본문 ]
    ///
    /// 길이를 앞에 붙이는 이유는 TCP가 경계를 보장하지 않기 때문입니다.
    /// </summary>
    public static class PacketHelper
    {
        /// <summary>한 패킷의 본문 최대 크기</summary>
        private const int MAX_BODY = 8192;

        // ═══════════════════════════════════════
        // 전송
        // ═══════════════════════════════════════
        public static async Task SendAsync(Socket socket, object packet)
        {
            string json = JsonConvert.SerializeObject(packet);
            byte[] body = Encoding.UTF8.GetBytes(json);

            byte[] full = new byte[4 + body.Length];
            BitConverter.GetBytes(body.Length).CopyTo(full, 0);
            body.CopyTo(full, 4);

            int sent = 0;
            while (sent < full.Length)
            {
                sent += await socket.SendAsync(
                    new ArraySegment<byte>(full, sent, full.Length - sent),
                    SocketFlags.None);
            }
        }

        // ═══════════════════════════════════════
        // 수신         ( Part B 부하 측정에서 접속 실패와 스레드 수 증가가 관찰되어, 이 부분 수정
        // ═══════════════════════════════════════
        public static async Task<string?> ReceiveAsync(Socket socket)
        {
            byte[] header = new byte[4];
            int received = 0;

            while (received < header.Length)
            {
                int n = await socket.ReceiveAsync(
                    new ArraySegment<byte>(
                        header, received, header.Length - received),
                    SocketFlags.None);

                if (n == 0) return null;

                received += n;
            }

            // 헤더에서 본문 길이를 읽고 검사한다.
            int bodyLength = BitConverter.ToInt32(header, 0);

            if (bodyLength <= 0 || bodyLength > MAX_BODY)
                return null;

            byte[] body = new byte[bodyLength];
            received = 0;

            while (received < body.Length)
            {
                int n = await socket.ReceiveAsync(
                    new ArraySegment<byte>(
                        body, received, body.Length - received),
                    SocketFlags.None);

                if (n == 0) return null;

                received += n;
            }

            return Encoding.UTF8.GetString(body, 0, body.Length);
        }
    }
}

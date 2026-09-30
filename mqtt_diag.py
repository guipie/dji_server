"""
极简 MQTT 订阅诊断脚本 —— 只看 reply 主题能不能收到
用法: python mqtt_diag.py
连接参数与 dji_server Dji.json 完全一致
"""
import sys, time, json
try:
    import paho.mqtt.client as mqtt
except ImportError:
    print("请先 pip install paho-mqtt")
    sys.exit(1)

BROKER = "172.18.100.38"
PORT = 1883
USERNAME = "JavaServer"
PASSWORD = "B88a4a98$4_d492@8c8#738dC00b#5C4"

# 用和后端一样的 ClientId 前缀 + 随机后缀（模拟后端）
import uuid
CLIENT_ID = f"dji_client_01_py_{uuid.uuid4().hex[:8]}"

# 同时订阅 reply 和 osd，对比是不是只有 reply 收不到
TOPICS = [
    ("thing/product/+/requests_reply", 0),
    ("thing/product/+/services_reply", 0),
    ("thing/product/+/property/set_reply", 0),
    ("thing/product/+/osd", 0),  # 对照组
    ("thing/product/+/events", 0),  # 对照组
]

reply_count = 0
osd_count = 0
other_count = 0

def on_connect(client, userdata, flags, rc, properties=None):
    if rc == 0:
        print(f"[OK] 已连接 {BROKER}:{PORT} client_id={CLIENT_ID}")
        for t, q in TOPICS:
            result, mid = client.subscribe(t, q)
            tag = "❌" if result != 0 else "✅"
            print(f"  {tag} 订阅 {t} mid={mid} result={result}")
    else:
        print(f"[FAIL] 连接被拒绝 rc={rc}")

def on_message(client, userdata, msg):
    global reply_count, osd_count, other_count
    topic = msg.topic
    payload = msg.payload.decode("utf-8", errors="replace")
    preview = payload[:200] + ("..." if len(payload) > 200 else "")

    if topic.endswith("_reply"):
        reply_count += 1
        print(f"\n[REPLY #{reply_count}] {topic}")
        print(f"  {preview}")
    elif topic.endswith("/osd"):
        osd_count += 1
        if osd_count <= 3 or osd_count % 50 == 0:
            print(f"[OSD #{osd_count}] {topic}")
    elif topic.endswith("/events"):
        other_count += 1
        print(f"[EVT  #{other_count}] {topic}")
    else:
        other_count += 1
        print(f"[MSG  #{other_count}] {topic}")

def on_subscribe(client, userdata, mid, reason_codes, properties=None):
    print(f"  SUBACK mid={mid} codes={reason_codes}")

client = mqtt.Client(mqtt.CallbackAPIVersion.VERSION2, client_id=CLIENT_ID, clean_session=True)
client.username_pw_set(USERNAME, PASSWORD)
client.on_connect = on_connect
client.on_message = on_message
client.on_subscribe = on_subscribe

try:
    client.connect(BROKER, PORT, keepalive=60)
    client.loop_start()
    print(f"\n等待消息中... 按 Ctrl+C 退出")
    print(f"请在后端调用 BindWorkspace 下发指令\n")

    start = time.time()
    while True:
        time.sleep(1)
except KeyboardInterrupt:
    print(f"\n\n=== 统计 ===")
    print(f"运行时间: {time.time() - start:.1f}s")
    print(f"OSD 消息:   {osd_count}")
    print(f"Reply 消息: {reply_count}")
    print(f"其他消息:   {other_count}")
    client.loop_stop()
    client.disconnect()

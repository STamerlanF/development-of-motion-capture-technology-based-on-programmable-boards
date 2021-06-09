#include "I2Cdev.h"
#include "MPU6050_6Axis_MotionApps_V6_12.h"
#include "Wire.h"

MPU6050 mpu;

// Vector(x, y, z)angles.
float angleX = 0;
float angleY = 0;
float angleZ = 0;

// Stored required properties.
const float toDeg = 180.0 / M_PI;
uint8_t mpuIntStatus;   // holds actual interrupt status byte from MPU
uint8_t devStatus;      // return status after each device operation (0 = success, !0 = error)
uint16_t packetSize;    // expected DMP packet size (default is 42 bytes)
uint16_t fifoCount;     // count of all bytes currently in FIFO
uint8_t fifoBuffer[64]; // FIFO storage buffer
Quaternion q;           // [w, x, y, z]         quaternion container
VectorFloat gravity;    // [x, y, z]            gravity vector
float ypr[3];           // [yaw, pitch, roll]   yaw/pitch/roll container and gravity vector



void setup() {
  Wire.begin();
  Wire.setClock(400000);
  Serial.begin(115200);

 Serial.println(F("Send any character to start..."));
  delay(100);
  while (1) {                //входим в бесконечный цикл
    if (Serial.available() > 0) {     //если нажата любая кнопка
      Serial.read();                  //прочитать (чтобы не висел в буфере)
      break;                          //выйти из цикла
    }
  }
  delay(1000);

  Serial.println("Begining...");
    delay(1000);  

  Serial.println("Sensor Initialization...");
  delay(1000);

  mpu.initialize();

  Serial.println("DMP Initialization...");
  delay(1000);

  initDMP();

  
}

void loop() {
  calculateAngles();
  Serial.print(angleX); Serial.print(',');
  Serial.print(angleY); Serial.print(',');
  Serial.println(angleZ);
  delay(1000);
}

// инициализация
void initDMP() {
  devStatus = mpu.dmpInitialize();
  mpu.setDMPEnabled(true);
  mpuIntStatus = mpu.getIntStatus();
  packetSize = mpu.dmpGetFIFOPacketSize();
}

// получение углов в angleX, angleY, angleZ
void calculateAngles() {
  if (mpu.dmpGetCurrentFIFOPacket(fifoBuffer)) {
    mpu.dmpGetQuaternion(&q, fifoBuffer);
    mpu.dmpGetGravity(&gravity, &q);
    mpu.dmpGetYawPitchRoll(ypr, &q, &gravity);
    angleX = ypr[2] * toDeg;
    angleY = ypr[1] * toDeg;
    angleZ = ypr[0] * toDeg;
  }
}

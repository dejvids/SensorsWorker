#include <Arduino.h>
#include <ArduinoJson.h>

#include "ESP8266WiFi.h"
#include "ESP8266WebServer.h"
#include "ESP8266HTTPClient.h"
#include "CronAlarms.h"
#include <stdio.h>
#include <stdlib.h>
#include <time.h>


ESP8266WebServer server;

String API_KEY = "";
const int MAX_ALARMS = 3;
char* ssid = "";
char* password = "";
uint8_t pin_led = BUILTIN_LED;
String timeApi = "http://worldtimeapi.org/api/ip";

char* ALARM_1 = "0 58 19 * * *";
char* ALARM_2 = "0 * * * * *";
char* ALARM_3 = "0 58 19 * * *";

struct SensorsData{
  float waterTemp;
  float roomTemp;
  float roomHumidity;
  float roomPressure;

};

struct AlarmItem {
  CronID_t id;
  String expression;
  bool repeat;
};

CronID_t alarms[MAX_ALARMS];

struct SensorsData getRandomData() {
    struct SensorsData data;
    data.roomTemp = ((float)rand()/(float)(RAND_MAX)) * 10 + 18;
    data.roomPressure = ((float)rand()/(float)(RAND_MAX)) * 100 + 1000;
    data.waterTemp = ((float)rand()/(float)(RAND_MAX)) * 20 + 10;
    data.roomHumidity = ((float)rand()/(float)(RAND_MAX)) * 50 + 20;

    return data;
}

void formatMessage(char* string, struct SensorsData data) {
    sprintf(string, "{\"waterTemp\":%f,\"roomTemp\":%f,\"roomHumidity\":%f,\"roomPressure\":%f}", data.waterTemp, data.roomTemp, data.roomHumidity, data.roomPressure);
}


void syncDateTime() {
  HTTPClient http;
  
  // Your Domain name with URL path or IP address with path
  http.begin(timeApi.c_str());
  
  // Send HTTP GET request
  int httpResponseCode = http.GET();
  
  if (httpResponseCode>0) {
    Serial.print("HTTP Response code: ");
    Serial.println(httpResponseCode);
    String payload = http.getString();
    Serial.println(payload);
    StaticJsonDocument<1500> doc;
    auto error = deserializeJson(doc, payload);
    if (error) {
      Serial.print(F("deserializeJson() failed with code "));
      Serial.println(error.c_str());
      return;
  }
    // Free resources
    http.end();
    const auto timestamp = doc["unixtime"].as<long>();
    auto utcOffsetString = doc["utc_offset"].as<String>();
    char offsetSign = utcOffsetString[0];

    Serial.println(utcOffsetString.substring(1,3));
    utcOffsetString = utcOffsetString.substring(1,3);
    
    int utcOffset = utcOffsetString.toInt();
    Serial.println(utcOffset);
    if(offsetSign == '-')
      utcOffset *= -1;

    time_t rawtime = timestamp;
    struct tm * timeinfo;
    timeinfo = localtime (&rawtime);
    (*timeinfo).tm_hour += utcOffset;
    Serial.print("Set internet time:");
    Serial.print(asctime(timeinfo));
    setDateTime(timeinfo);
  }
  else {
    Serial.print("Error code: ");
    Serial.println(httpResponseCode);
  }
}

bool authenticate(){
  bool isAuthenticated = server.hasArg("api_key") && server.arg("api_key").equals(API_KEY);
  if(!isAuthenticated) {
    Serial.println("Unathorized");
    server.send(401, "text/plain", "missing or invalid api_key");
  }

  return isAuthenticated;
}

void onToggleLed()
{
  //if(!authenticate())
    //return;

  digitalWrite(pin_led, !digitalRead(pin_led));
  server.send(204, "");
}

void onSensorsRequested() {
  if(!authenticate())
    return;

  struct SensorsData data = getRandomData();
  char* json = (char*)malloc(100 * sizeof(char));
  formatMessage(json, data);
  Serial.println(json);
  server.send(200, "application/json", json);
  free(json);   
}

void onDateTimeUpdated() {
  
  if(!authenticate())
    return;

  StaticJsonDocument<300> doc;
  auto error = deserializeJson(doc, server.arg("plain"));
  
  if (error) {
      Serial.print(F("deserializeJson() failed with code "));
      Serial.println(error.c_str());
      return;
  }

  struct tm tm_newtime;
  tm_newtime.tm_year = doc["year"].as<int>() - 1900;
  tm_newtime.tm_mon = doc["month"].as<int>()- 1;
  tm_newtime.tm_mday = doc["day"].as<int>();
  tm_newtime.tm_hour = doc["hour"].as<int>();
  tm_newtime.tm_min = doc["minute"].as<int>();
  tm_newtime.tm_sec = 0;
  tm_newtime.tm_isdst = 0;

  setDateTime(&tm_newtime);

  server.send(204, "");
}

void setDateTime(struct tm * tm_newtime){
    timeval tv = { mktime(tm_newtime), 0 };
    settimeofday(&tv, nullptr);
    Cron.free(alarms[0]);
    Cron.free(alarms[1]);
    setupScheduler();
}

void setInitDateTime(){
    struct tm tm_newtime; // set time to Saturday 8:29:00am Jan 1 2011
    tm_newtime.tm_year = 2021 - 1900;
    tm_newtime.tm_mon = 4 - 1;
    tm_newtime.tm_mday = 00;
    tm_newtime.tm_hour = 16;
    tm_newtime.tm_min = 00;
    tm_newtime.tm_sec = 0;
    tm_newtime.tm_isdst = 0;

    setDateTime(&tm_newtime);
}

void setupScheduler() {
  Serial.println("Starting scheduler setup");

  alarms[0] = Cron.create(ALARM_1, alarm1, false);
  alarms[1] = Cron.create(ALARM_2, alarm2, false);
  alarms[1] = Cron.create(ALARM_3, alarm3, false);

  Serial.println("End scheduler setup");
}

void alarm1(){}
void alarm2(){}
void alarm3(){}

void printCurrentTime(){
  time_t tnow = time(nullptr);
  Serial.print("Current time: ");
  Serial.print(asctime(gmtime(&tnow)));
}

void onGetDateTime(){
  printCurrentTime();
  time_t tnow = time(nullptr);
  server.send(200, "text/plain", asctime(gmtime(&tnow)));
}

void onGetAlarms(){
  
}

void configureEndpoints(){
  server.on("/",[](){server.send(200,"text/plain","ESP8266 web server");});
  server.arg("api_key");
  server.on("/sensors", HTTP_GET, onSensorsRequested);
  server.on("/tled",onToggleLed);
  server.on("/datetime", HTTP_POST, onDateTimeUpdated);
  server.on("/datetime", HTTP_GET, onGetDateTime);
  server.on("/alarms", HTTP_GET, onGetAlarms);
}

void setup()
{
  Serial.begin(115200);
  Serial.println("Scheduler setup");
  setupScheduler();
  int i, seed;
  time_t tt;
  seed = time(&tt);
  
  srand(seed);  

  pinMode(pin_led, OUTPUT);
  WiFi.begin(ssid, password);
  Serial.print("Connecting server ");

  while(WiFi.status()!=WL_CONNECTED)
  {
    Serial.print(".");
    delay(500);
  }
  
  Serial.println("");
  Serial.print("IP Adress: ");
  Serial.println(WiFi.localIP());
  Serial.print("MAC Address:"); // Use to fix IP on router
  Serial.println(WiFi.macAddress());

  configureEndpoints();
  server.begin();

  syncDateTime();
}

void loop() {
  server.handleClient();
  Cron.delay();
}

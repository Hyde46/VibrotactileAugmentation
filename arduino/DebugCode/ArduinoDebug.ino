#include <SoftwareSerial.h>


 
void setup() {
 Serial.begin(9600);
 pinMode(3, OUTPUT);
}

void loop () {

  if (Serial.available() > 0){
  String in=Serial.readString();
  String on ="ON";
  String off="OFF";
  if(in==on){
        digitalWrite(3, HIGH);      
    }else if (in.equals(off)){
        digitalWrite(3,LOW);
    }else{
      //Serial.println(in);
    }
    Serial.println(in);
    }         
}


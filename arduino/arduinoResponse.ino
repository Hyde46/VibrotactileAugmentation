//pins for the output
int pins[5] = {3, 5, 6, 9, 10};
int inputs[5];
int max = 255;
bool isFound = false;
String side = "Left";
String respones;
bool on = true;
void setup () {
  //baudrate
  Serial.begin(9600);
  // pinmodes
  for(int i=0;i<5;i++){
    pinMode(pins[i], OUTPUT);
  }
  pinMode(9, OUTPUT);
}

  void loop() {
  // input available?
  while(Serial.available()>0){
  /*
      if (Serial.readString().equals("Side?")){
        isFound=true;
        Serial.write("Left");
      }
  */
    //get input and set the range
    for(int i=0;i<5;i++){
      inputs[i]=constrain(Serial.parseInt(),0,100);
    }
    //send input to the pins
    for(int i=0;i<5;i++){
      analogWrite(pins[i], (inputs[i]*max)/100);
    }
  }

  }

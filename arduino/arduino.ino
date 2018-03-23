//pins for the output
int pins[]={3,5,6,9,10};
int inputs[5];
int max=255;
void setup () {
  //baudrate
  Serial.begin(9600);
  // pinmodes
  for(int i=0;i<5;i++){
    pinMode(pins[i], OUTPUT);
  }
}

void loop() {
  // input available?
  while(Serial.available()>0){
    //get input and set the range
    for(int i=0;i<5;i++){
      inputs[i]=constrain(Serial.parseInt(),0,100);
    }
    //send input to the pins
    for(int i=0;i<5;i++){
      analogWrite(pins[i], (input[i]*max)/100);
    }
  }

}

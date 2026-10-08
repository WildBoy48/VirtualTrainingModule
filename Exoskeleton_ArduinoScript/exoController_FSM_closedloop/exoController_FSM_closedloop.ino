// -- Pin Definitions --
const uint8_t PUMP_PIN = 39;

const uint8_t VALVE_PINS[] = {31, 25, 27, 29, 23}; // Switched 31 and 23 so that it is able to reach the correct valve
const uint8_t NUM_VALVES   = 5;
const char* VALVE_NAMES[]  = {"Thumb Valve", "Index Valve", "Middle Valve", "Ring Valve", "Pinky Valve"};

const uint8_t SENSOR_PINS[] = {A0, A1, A2, A3, A4};
const uint8_t NUM_SENSORS   = 5;

// -- FSM Definition --
enum ExoskeletonState {
    STATE_IDLE,           // System is on, but doing nothing
    STATE_GRABBING,       // Valves inflate, pump on
    STATE_RELEASING,      // Valves venting, pump off
    STATE_ERROR_TIMEOUT   // Heartbeat lost, emergency vent
};

enum FingerState{
  STATE_VENTING,          // Finger is venting towards open air
  STATE_INFLATING,        // Finger is inflating, valve permits passage to glove & sensor
  STATE_HOLDING           // Finger has reached necessary pressure, trap the air
};

// Start in the IDLE state
ExoskeletonState currentState = STATE_IDLE;
FingerState fingerStates[NUM_VALVES];
const float MAX_SAFE_KPA = 125.0;   // According to datasheet, 120kPa is pressure needed to inflate

// -- Heartbeat Variables --
unsigned long lastMessageTime = 0;
const unsigned long TIMEOUT_MS = 1000;
bool isConnected = false;

// -- Hardware & Telemetry Variables --
unsigned long lastSensorTime = 0;
const unsigned long SENSOR_INTERVAL = 50;  // Send data every 50ms (20Hz)
float pressureOffsets[NUM_SENSORS];        // Auto-tare at boot

void setup() {
    Serial.begin(115200);

    // Initialize all pins to LOW
    for (int i = 0; i < NUM_VALVES; i++){
        digitalWrite(VALVE_PINS[i], LOW);
        pinMode(VALVE_PINS[i], OUTPUT);
    }
    
    digitalWrite(PUMP_PIN, LOW);
    pinMode(PUMP_PIN, OUTPUT);

    // Setting Sensors and calibrating them
    for (int i = 0; i < NUM_SENSORS; i++){
        pinMode(SENSOR_PINS[i], INPUT);

        int rawBootValue = analogRead(SENSOR_PINS[i]);
        float bootRatio = rawBootValue / 1023.0;
        pressureOffsets[i] = ((bootRatio - 0.1) / 0.75) * 1000.0;

        Serial.print("Sensor ");
        Serial.print(i);
        Serial.print(" Zero Offset: ");
        Serial.println(pressureOffsets[i]);
    }
    
    Serial.println("Exoskeleton FSM Initialized. Current State: IDLE");
    Serial.println("Waiting for connection...");
}

void loop() {
    // Non-blocking check for incoming serial commands
    if (Serial.available() > 0) {
        char command = Serial.read();

        if(command == 'G' || command == 'R' || command == 'H'){
            // Any valid command acts as a heartbeat
            lastMessageTime = millis();
            
            // If we were disconnected or in an error state, recover safely
            if(!isConnected){
                isConnected = true;
                Serial.println("Connection Established.");
                if (currentState == STATE_ERROR_TIMEOUT) {
                    changeState(STATE_IDLE); 
                }
            }

            // Route the command to the FSM transition logic
            processCommand(command);
        }
    }

    // Telemetry: Calculate kPa and actuate fingers
    if (isConnected && (millis() - lastSensorTime >= SENSOR_INTERVAL)) {

      bool pumpNeedstoRun = false;
      
      for(int i = 0; i < NUM_SENSORS; i++){
          int rawSensorValue = analogRead(SENSOR_PINS[i]);

          float voltageRatio = rawSensorValue / 1023.0;
          float rawKPa = ((voltageRatio - 0.1) / 0.75) * 1000.0;
          float finalKPa = rawKPa - pressureOffsets[i];
          
          if (finalKPa < 0)finalKPa = 0.0;


          // STATE INFLATING
          if(fingerStates[i] == STATE_INFLATING){
            if (finalKPa >= MAX_SAFE_KPA){
              // Time to seal the valve and hold the air in
              fingerStates[i] = STATE_HOLDING;
              Serial.print("Finger "); Serial.print(i);
              Serial.println(" reached MAX Pressure. Shifting to Holding");
            } else{
              // Stil under the limit
              pumpNeedstoRun = true;
            }
          }
          // STATES VENTING AND HOLDING
          if (fingerStates[i] == STATE_VENTING){
            digitalWrite(VALVE_PINS[i], LOW);
          } else{
            digitalWrite(VALVE_PINS[i], HIGH);
          }

          // Send Telemetry
          Serial.print("S");
          Serial.print(i);
          Serial.print(":"); 
          Serial.println(finalKPa);
      }
      digitalWrite(PUMP_PIN, pumpNeedstoRun ? HIGH : LOW);        
      lastSensorTime = millis();
    }

    // 3. Heartbeat Failsafe (FSM Transition Trigger)
    if(isConnected && (millis() - lastMessageTime > TIMEOUT_MS)){
        changeState(STATE_ERROR_TIMEOUT);
    }
}

// --- FSM Transition Logic ---

void processCommand(char cmd) {
    switch (cmd) {
        case 'G':
            changeState(STATE_GRABBING);
            break;
            
        case 'R':
            changeState(STATE_RELEASING);
            break;
            
        case 'H':
            // Heartbeat just updates the timer (handled in loop), no state change needed
            break;
    }
}

// The Gatekeeper: All hardware changes MUST pass through here
void changeState(ExoskeletonState newState) {
    // Don't do anything if we are already in the requested state
    if (currentState == newState) {
        return; 
    }

    currentState = newState;

    switch (currentState) {
        case STATE_IDLE:
            Serial.println(">>> STATE SHIFT: IDLE (System waiting)");
            for (int i = 0; i < NUM_VALVES; i++) fingerStates[i] = STATE_VENTING;
            break;

        case STATE_GRABBING:
            Serial.println(">>> STATE SHIFT: GRABBING (Active Flexion / Inflating)");
            for (int i = 0; i < NUM_VALVES; i++) fingerStates[i] = STATE_INFLATING;
            break;

        case STATE_RELEASING:
            Serial.println(">>> STATE SHIFT: RELEASING (Passive Extension / Venting)");
            // Valves must go HIGH (close) to trap air, Pump goes HIGH to push air
            for (int i = 0; i < NUM_VALVES; i++) fingerStates[i] = STATE_VENTING;
            break;

        case STATE_ERROR_TIMEOUT:
            Serial.println("!!! STATE SHIFT: ERROR TIMEOUT (Connection lost. Forcing vent!)");
            for (int i = 0; i < NUM_VALVES; i++) fingerStates[i] = STATE_VENTING;
            isConnected = false;
            break;
    }
}
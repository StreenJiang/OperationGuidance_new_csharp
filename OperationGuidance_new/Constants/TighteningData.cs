namespace OperationGuidance_new.Constants {
    // Revision 3 (plus revision 2) for PF4000
    public class TighteningData {
        // Revision 2 part
        public int cell_id { get; set; }                    	    // 23 - 26
        public int channel_id { get; set; }                 	    // 29 - 30
        public string torque_controller_name { get; set; }   	    // 33 - 57
        public string vin_number { get; set; }              	    // 60 - 84
        public int job_id { get; set; }                     	    // 87 - 90
        public int parameter_set_number { get; set; }        	    // 93 - 95

        public int strategy { get; set; }                  	    // 98 - 99
            // 01=Torque control, 
            // 02=Torque control / angle monitoring,
            // 03=Torque control / angle control AND,
            // 04=Angle control / torque monitoring, 
            // 05=DS control, 
            // 06=DS control torque monitoring, 
            // 07=Reverse angle, 
            // 08=Reverse torque, 
            // 09=Click wrench, 
            // 10=Rotate spindle forward, 
            // 11=Torque control angle control OR,
            // 12=Rotate spindle reverse, 
            // 13=Home position forward,
            // 14=EP Monitoring, 
            // 15=Yield, 
            // 16=EP Fixed, 
            // 17=EP Control,
            // 18=EP Angle shutoff, 
            // 19=Yield / torque control OR,
            // 20=Snug gradient, 
            // 21=Residual torque / Time
            // 22=Residual torque / Angle, 
            // 23=Breakaway peak
            // 24=Loose and tightening, 
            // 25=Home position reverse,
            // 26=PVT comp with Snug
            // 99=No strategy
        public int strategy_options { get; set; }           	    // 102 - 106
            // Bit 0 Torque
            // Bit 1 Angle
            // Bit 2 Batch
            // Bit 3 PVT Monitoring
            // Bit 4 PVT Compensate
            // Bit 5 Self-tap
            // Bit 6 Rundown
            // Bit 7 CM 
            // Bit 8 DS control
            // Bit 9 Click Wrench
            // Bit 10 RBW Monitoring

        public int batch_size { get; set; }                 	    // 109 - 112
        public int batch_counter { get; set; }              	    // 115 - 118

        public int tightening_status { get; set; }          	    // 121: 0=tightening NOK, 1=tightening OK
        public int batch_status { get; set; }               	    // 124: 0=batch NOK, 1=batch OK, 2=batch not used
        public int torque_status { get; set; }                     // 127: 0=Low, 1=OK, 2=High
        public int angle_status { get; set; }                      // 130: 0=Low, 1=OK, 2=High
        public int rundown_status { get; set; }                    // 133: 0=Low, 1=OK, 2=High
        public int current_monitoring_status { get; set; }         // 136: 0=Low, 1=OK, 2=High
        public int self_tap_status { get; set; }                   // 139: 0=Low, 1=OK, 2=High
        public int prevail_torque_monitoring_status { get; set; }  // 142: 0=Low, 1=OK, 2=High
        public int prevail_torque_compensate_status { get; set; }  // 145: 0=Low, 1=OK, 2=High
        public int tightening_error_status { get; set; }           // 148 - 157: as below
            // Bit 1 Rundown angle max shut off
            // Bit 2 Rundown angle min shut off
            // Bit 3 Torque max shut off
            // Bit 4 Angle max shut off
            // Bit 5 Self-tap torque max shut off
            // Bit 6 Self-tap torque min shut off
            // Bit 7 Prevail torque max shut off
            // Bit 8 Prevail torque min shut off
            // Bit 9 Prevail torque compensate overflow
            // Bit 10 Current monitoring max shut off
            // Bit 11 Post view torque min torque shut off
            // Bit 12 Post view torque max torque shut off
            // Bit 13 Post view torque Angle too small
            // Bit 14 Trigger lost
            // Bit 15 Torque less than target
            // Bit 16 Tool hot
            // Bit 17 Multistage abort
            // Bit 18 Rehit
            // Bit 19 DS measure failed
            // Bit 20 Current limit reached
            // Bit 21 End Time out shutoff
            // Bit 22 Remove fastener limit exceeded
            // Bit 23 Disable drive
            // Bit 24 Transducer lost
            // Bit 25 Transducer shorted
            // Bit 26 Transducer corrupt
            // Bit 27 Sync timeout
            // Bit 28 Dynamic current monitoring min
            // Bit 29 Dynamic current monitoring max
            // Bit 30 Angle max monitor
            // Bit 31 Yield nut off
            // Bit 32 Yield too few samples

        public float torque_min_limit { get; set; }                // 160 - 165: need to devided by 100
        public float torque_max_limit { get; set; }                // 168 - 173: need to devided by 100
        public float torque_final_target { get; set; }             // 176 - 181: need to devided by 100
        public float torque { get; set; }                          // 184 - 189: need to devided by 100

        public int angle_min { get; set; }                         // 192 - 196
        public int angle_max { get; set; }                         // 199 - 203
        public int angle_final_target { get; set; }                // 206 - 210
        public int angle { get; set; }                             // 213 - 217

        public int rundown_angle_min { get; set; }                 // 220 - 224
        public int rundown_angle_max { get; set; }                 // 227 - 231
        public int rundown_angle { get; set; }                     // 234 - 238

        public int current_monitoring_min { get; set; }            // 241 - 243
        public int current_monitoring_max { get; set; }            // 246 - 248
        public int current_monitoring_value { get; set; }          // 251 - 253

        public float self_tap_min { get; set; }                    // 256 - 261: need to devided by 100
        public float self_tap_max { get; set; }                    // 264 - 269: need to devided by 100
        public float self_tap_torque { get; set; }                 // 272 - 277: need to devided by 100

        public float prevail_torque_monitoring_min { get; set; }   // 280 - 285: need to devided by 100
        public float prevail_torque_monitoring_max { get; set; }   // 288 - 293: need to devided by 100
        public float prevail_torque { get; set; }                  // 296 - 301: need to devided by 100

        public int tightening_id { get; set; }                     // 304 - 311
        public int job_sequence_number { get; set; }               // 316 - 320
        public int sync_tightening_id { get; set; }                // 323 - 327
        public string tool_serial_number { get; set; }             // 330 - 343

        public string timestamp { get; set; }                      // 346 - 364: YYYY-MM-DD:HH:MM:SS
        // 367 - 385: YYYY-MM-DD:HH:MM:SS
        public string date_or_time_of_last_change_in_parameter_set_settings { get; set; }

        // Revision 3 part
        public string parameter_set_name { get; set; }             // 388 - 412
        public int torque_values_unit { get; set; }                // 415: 1=Nm, 2=Lbf, 3=Lbf.ln, 4=Kpm, 5=Kgf.cm, 6=ozf.in, 7=%, 8=Ncm
        // 418 - 419: 1=Tightening, 2=Loosening, 3=Batch Increment, 4=Batch Decrement, 5=Bypass parameter set result, 6=Abort Job result, 7=Sync tightening, 8=Reference setup
        public int result_type { get; set; }                        
    }
}

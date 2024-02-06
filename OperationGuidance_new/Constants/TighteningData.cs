namespace OperationGuidance_new.Constants {
    // Revision 3 (plus revision 2) for PF4000
    public class TighteningData {
        // Revision 2 part
        public int? CellID { get; set; }                    	// 23 - 26
        public int? ChannelID { get; set; }                 	// 29 - 30
        public string? TorqueControllerName { get; set; }   	// 33 - 57
        public string? VINNumber { get; set; }              	// 60 - 84
        public int? JobID { get; set; }                     	// 87 - 90
        public int? ParameterSetNumber { get; set; }        	// 93 - 95

        public int? Strategy { get; set; }                  	// 98 - 99
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
        public int? StrategyOptions { get; set; }           	// 102 - 106
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

        public int? BatchSize { get; set; }                 	// 109 - 112
        public int? BatchCounter { get; set; }              	// 115 - 118

        public int? TighteningStatus { get; set; }          	// 121: 0=tightening NOK, 1=tightening OK
        public int? BatchStatus { get; set; }               	// 124: 0=batch NOK, 1=batch OK, 2=batch not used
        public int? TorqueStatus { get; set; }                  // 127: 0=Low, 1=OK, 2=High
        public int? AngleStatus { get; set; }                   // 130: 0=Low, 1=OK, 2=High
        public int? RundownAngleStatus { get; set; }            // 133: 0=Low, 1=OK, 2=High
        public int? CurrentMonitoringStatu { get; set; }        // 136: 0=Low, 1=OK, 2=High
        public int? SelfTapStatus { get; set; }                 // 139: 0=Low, 1=OK, 2=High
        public int? PrevailTorqueMonitoringStatus { get; set; } // 142: 0=Low, 1=OK, 2=High
        public int? PrevailTorqueCompensateStatus { get; set; } // 145: 0=Low, 1=OK, 2=High
        public int? TighteningErrorStatus { get; set; }         // 148 - 157: as below
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

        public float? TorqueMinLimit { get; set; }              // 160 - 165: need to devided by 100
        public float? TorqueMaxLimit { get; set; }              // 168 - 173: need to devided by 100
        public float? TorqueFinalTarget { get; set; }           // 176 - 181: need to devided by 100
        public float? Torque { get; set; }                      // 184 - 189: need to devided by 100

        public int? AngleMin { get; set; }                      // 192 - 196
        public int? AngleMax { get; set; }                      // 199 - 203
        public int? AngleFinalTarget { get; set; }              // 206 - 210
        public int? Angle { get; set; }                         // 213 - 217

        public int? RundownAngleMin { get; set; }               // 220 - 224
        public int? RundownAngleMax { get; set; }               // 227 - 231
        public int? RundownAngle { get; set; }                  // 234 - 238

        public int? CurrentMonitoringMin { get; set; }          // 241 - 243
        public int? CurrentMonitoringMax { get; set; }          // 246 - 248
        public int? CurrentMonitoringValue { get; set; }        // 251 - 253

        public float? SelfTapMin { get; set; }                  // 256 - 261: need to devided by 100
        public float? SelfTapMax { get; set; }                  // 264 - 269: need to devided by 100
        public float? SelfTapTorque { get; set; }               // 272 - 277: need to devided by 100

        public float? PrevailTorqueMonitoringMin { get; set; }  // 280 - 285: need to devided by 100
        public float? PrevailTorqueMonitoringMax { get; set; }  // 288 - 293: need to devided by 100
        public float? PrevailTorque { get; set; }               // 296 - 301: need to devided by 100

        public int? TighteningID { get; set; }                  // 304 - 311
        public int? JobSequenceNumber { get; set; }             // 316 - 320
        public int? SyncTighteningID { get; set; }              // 323 - 327
        public string? ToolSerialNumber { get; set; }           // 330 - 343

        public string? TimeStamp { get; set; }                  // 346 - 364: YYYY-MM-DD:HH:MM:SS
        // 367 - 385: YYYY-MM-DD:HH:MM:SS
        public string? DateOrTimeOfLastChangeInParameterSetSettings { get; set; }

        // Revision 3 part
        public string? ParameterSetName { get; set; }           // 388 - 412
        public int? TorqueValuesUnit { get; set; }              // 415: 1=Nm, 2=Lbf, 3=Lbf.ln, 4=Kpm, 5=Kgf.cm, 6=ozf.in, 7=%, 8=Ncm
        // 418 - 419: 1=Tightening, 2=Loosening, 3=Batch Increment, 4=Batch Decrement, 5=Bypass parameter set result, 6=Abort Job result, 7=Sync tightening, 8=Reference setup
        public int? ResultType { get; set; }                        
    }
}

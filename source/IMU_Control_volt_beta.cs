using UnityEngine;
using System.Collections;
using System.IO.Ports;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;

public class IMU_Control_volt_beta: MonoBehaviour
{	
	public static SerialPort serialPort = new SerialPort ("COM5", 115200);
	GameObject IMU_Device;	//Object used for showing orietation changing in real-time
	Quaternion IMU_DeviceQuat = Quaternion.identity; //Quaternion is used to represent orietation
	float []q=new float[5]; //q0,q1,q2,q3,v //Data from IMU includes quternion (q0,q1,q2,q3) and voltage v
	GameObject VoltText; // text object to show voltage in real-time
	TextMesh textMesh_volt; // text mesh 
	float Voltage; //voltage value
	bool startMark = false; //flag of start running
	bool loseconnection=false; //flag of losing bluetooth connection
	Thread uartThread; //Second Thread to read data from serial port 

	//Inititalize the scene
	void Start () 
	{
		OpenConnection ();	// Build the connection
		IMU_Device = GameObject.Find("IMU_Device"); //find object to be updated
		IMU_DeviceQuat = IMU_Device.transform.rotation;//retrive the orietation from the object
		VoltText = GameObject.Find ("Volt"); //find the text object
		textMesh_volt = VoltText.GetComponent <TextMesh> (); // assign the mesh
		ThreadStart mThreadStart = new ThreadStart (UARTStreamer);//define a new thread for streaming data		
		uartThread = new Thread (mThreadStart); 		
		uartThread.Start ();//start the new thread for streaming data
	}

	//Update scene
	void Update() 
	{
		IMU_Device.transform.rotation = IMU_DeviceQuat; //Assign the latest orietation data to the object
		if (loseconnection == true) //Verify the bluetooth connection every frame
			return; //if lost the connection then do nothing
		else //if conenction works well then update the voltage value
			textMesh_volt.text = string.Format ("Voltage {0} V", Voltage);
	}

	//this is where to stream the orietation data through serial port
	void UARTStreamer () {
		while(true) {	
			try {			
				int LineSep = serialPort.ReadChar();
				//Each data package always starts with letter 'Q'
				if (LineSep == 'Q')
				{
					int letter = serialPort.ReadChar(); //","//The second bit is ','
					bool minus=false;
					char buf;
					int k=0;
					while(k<5)//read in all five values q0, q1,q2,q3,v
					{
						minus = false;
						q[k]=0;
						buf = (char)serialPort.ReadChar();//read q0
						if(buf == '-')//check if it is a negative number
						{
							minus=true; //negative number begins with '-'
							buf=(char)serialPort.ReadChar();// read the char after '-'
						}
						int d =buf-'0';//convert char to int
						q[k]+=d; //d is the ones place number
						buf=(char)serialPort.ReadChar();//'.' decimal point
						for (int j=1;j<=3;j++) // three digits after the decimal point
						{	
							//read in 3 chars and covert the string to a float value
							buf=(char)serialPort.ReadChar();
							d=buf-'0';
							q[k]+=(float)(d/(Math.Pow(10,j)));
						}
						if(minus==true) //if negative value
							q[k]*=-1;
						k++;
						if (k<5)
						{
							//',' is the dividen char between each data
							buf=(char)serialPort.ReadChar(); //','
						}
						else
							//this is the last char for line change
							buf=(char)serialPort.ReadChar(); //'enter'
					}
				}
				//IMU to Unity coordinate conversion
				Quaternion quat_unity = new Quaternion(q[1],-q[3],q[2],q[0]);
				IMU_DeviceQuat = quat_unity;
				//vread voltage
				Voltage = q[4];
				loseconnection = false;
				if (startMark == false)
					startMark = true;
			} catch (TimeoutException) {

				loseconnection = true;
				print ("TIMEOUT");
				
			} catch (FormatException) {
				loseconnection = true;
				print ("INCORRECT FORMAT");
			}
		}
	}
	void OpenConnection() {
		if (serialPort != null) {
			if (serialPort.IsOpen) {	
				serialPort.Close ();
				print ("Closing port because it was already open!");
			} else {
				serialPort.Open ();	
				serialPort.ReadTimeout = 40; // 25Hz	
				print ("Port opened");
			}
		} else {
			if (serialPort.IsOpen) {
				print ("Port is already open!");	
			} else {
				print ("Port is null!");	
			}
		}
	}
}
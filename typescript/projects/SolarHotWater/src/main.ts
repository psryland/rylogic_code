import { app, BrowserWindow } from 'electron';
import ewelink from 'ewelink-api';

//const zc = require('ewelink-api/src/classes/Zeroconf');

// This method will be called when Electron has finished initialization and is ready to create browser windows.
// Some APIs can only be used after this event occurs.
app.whenReady().then(() => {

	// Create the browser window.
	const mainWindow = new BrowserWindow({
		width: 400,
		height: 400,
		darkTheme: true
		//webPreferences: {
		//	preload: path.join(__dirname, 'preload.js')
		//}
	});

	// Hide the main menu
	//mainWindow.setMenu(null);

	// and load the index.html of the app.
	mainWindow.loadFile('index.html');

	// Open the DevTools.
	// mainWindow.webContents.openDevTools()	

	app.on('activate', function () {
		// // On macOS it's common to re-create a window in the app when the
		// // dock icon is clicked and there are no other windows open.
		// if (BrowserWindow.getAllWindows().length === 0)
		// 	createWindow()
	});


	(async () => {

		const ewe = new ewelink({
			email: 'psryland@yahoo.co.nz',
			password: 'WLArs3F4ce',
			//region: 'us',
		});

		//await zc.saveArpTable({
		//	ip: '192.168.100.134'
		//});
		//await ewe.saveDevicesCache();
		
		//const region = await ewe.getRegion();
		//console.log(region);
	
		// get all devices
		//const devices = await ewe.getDevices();
		//console.log(devices);

		// get specific devide info
		const device = await ewe.getDevice('1000890fbb');
		console.log(device);
	  
		// toggle device
		//await ewe.toggleDevice('<your device id>');
	
	})();

});

// Quit when all windows are closed, except on macOS. There, it's common
// for applications and their menu bar to stay active until the user quits
// explicitly with Cmd + Q.
app.on('window-all-closed', function () {
	//if (process.platform !== 'darwin')
	app.quit()
});

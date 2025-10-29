// import * as vscode from "vscode";
// import { exec, ChildProcess } from "child_process";
// import * as net from "net";
// import { findCSharplyExecutable, log, wait } from "./utils";

// export async function organizeFileCommand() {
//   const activeEditor = vscode.window.activeTextEditor;

//   if (!activeEditor) {
//     vscode.window.showErrorMessage("No active file to organize");
//     return;
//   }

//   const path = activeEditor.document.uri.fsPath;
//   if (!path.endsWith(".cs")) {
//     // vscode.window.showWarningMessage(
//     //   "CSharply can only organize C# files (.cs)"
//     // );
//     return;
//   }

//   // Save the current file before processing (only if modified)
//   if (activeEditor.document.isDirty) {
//     try {
//       await activeEditor.document.save();
//     } catch (saveError) {
//       vscode.window.showErrorMessage(`Failed to save file: ${saveError}`);
//       return;
//     }
//   }

//   try {
//     log("Organizing C# file via daemon...");
//     // Get the file contents
//     const fileContents = activeEditor.document.getText();
//     const organizedCode = await organizeCode(fileContents);

//     // Update the file content with the organized code
//     if (organizedCode && organizedCode !== fileContents) {
//       await activeEditor.edit((editBuilder) => {
//         editBuilder.replace(
//           new vscode.Range(0, 0, activeEditor.document.lineCount, 0),
//           organizedCode
//         );
//       });

//       await activeEditor.document.save();
//       log("File organized successfully via daemon");
//       vscode.window.showInformationMessage(
//         "CSharply: File organized successfully"
//       );
//     } else {
//       log("No changes made to file");
//     }
//   } catch (error) {
//     log(`Error organizing file via daemon: ${error}`);
//     vscode.window.showErrorMessage(`CSharply error: ${error}`);
//   }
// }

// // Module-level state
// let daemonProcess: ChildProcess | undefined;
// let isStarting = false;
// const pipeName = "\\\\.\\pipe\\csharply";

// async function isPipeAvailable(): Promise<boolean> {
//   return new Promise((resolve) => {
//     const testClient = net.createConnection(pipeName);

//     const timeout = setTimeout(() => {
//       if (!testClient.destroyed) {
//         testClient.destroy();
//       }
//       resolve(false);
//     }, 1000);

//     testClient.on("connect", () => {
//       clearTimeout(timeout);
//       testClient.destroy();
//       resolve(true);
//     });

//     testClient.on("error", () => {
//       clearTimeout(timeout);
//       if (!testClient.destroyed) {
//         testClient.destroy();
//       }
//       resolve(false);
//     });
//   });
// }

// async function organizeCode(code: string): Promise<string> {
//   // Ensure daemon is running
//   if (!isRunning()) {
//     await start();
//   }

//   // Wait for pipe to be available
//   const maxRetries = 10;
//   let retries = 0;
//   while (retries < maxRetries && !(await isPipeAvailable())) {
//     await new Promise((resolve) => setTimeout(resolve, 500));
//     retries++;
//   }

//   if (retries >= maxRetries) {
//     throw new Error("Daemon pipe is not available after multiple retries");
//   }

//   return new Promise((resolve, reject) => {
//     const client = net.createConnection(pipeName);
//     let response = "";
//     let hasConnected = false;
//     let isResolved = false;

//     const cleanup = () => {
//       if (timeout) {
//         clearTimeout(timeout);
//       }
//       if (client && !client.destroyed) {
//         client.destroy();
//       }
//     };

//     const timeout = setTimeout(() => {
//       if (!hasConnected && !isResolved) {
//         isResolved = true;
//         cleanup();
//         reject(new Error("Connection timeout - daemon may not be ready"));
//       }
//     }, 5000);

//     client.on("connect", () => {
//       hasConnected = true;
//       clearTimeout(timeout);

//       try {
//         // Check if client is still writable before sending data
//         if (client.writable && !client.destroyed) {
//           client.write(code);
//           client.end();
//         } else {
//           if (!isResolved) {
//             isResolved = true;
//             cleanup();
//             reject(new Error("Client connection is not writable"));
//           }
//         }
//       } catch (writeError) {
//         if (!isResolved) {
//           isResolved = true;
//           cleanup();
//           reject(
//             new Error(
//               `Failed to write to daemon: ${
//                 writeError instanceof Error
//                   ? writeError.message
//                   : String(writeError)
//               }`
//             )
//           );
//         }
//       }
//     });

//     client.on("data", (data) => {
//       response += data.toString();
//     });

//     client.on("end", () => {
//       if (!isResolved) {
//         isResolved = true;
//         cleanup();
//         resolve(response);
//       }
//     });

//     client.on("error", async (error) => {
//       if (isResolved) {
//         return;
//       }

//       cleanup();

//       // Handle EPIPE errors and other connection issues
//       if (
//         !hasConnected &&
//         (error.message.includes("ENOENT") ||
//           error.message.includes("connect") ||
//           error.message.includes("EPIPE"))
//       ) {
//         try {
//           log("Daemon connection failed, attempting to restart...");
//           daemonProcess = undefined; // Reset process reference
//           await start();

//           // Retry the connection with proper error handling
//           const retryClient = net.createConnection(pipeName);
//           let retryResponse = "";
//           let retryConnected = false;
//           let retryResolved = false;

//           const retryCleanup = () => {
//             if (retryTimeout) {
//               clearTimeout(retryTimeout);
//             }
//             if (retryClient && !retryClient.destroyed) {
//               retryClient.destroy();
//             }
//           };

//           const retryTimeout = setTimeout(() => {
//             if (!retryResolved) {
//               retryResolved = true;
//               retryCleanup();
//               reject(new Error("Retry connection timeout"));
//             }
//           }, 5000);

//           retryClient.on("connect", () => {
//             retryConnected = true;
//             clearTimeout(retryTimeout);

//             try {
//               if (retryClient.writable && !retryClient.destroyed) {
//                 retryClient.write(code);
//                 retryClient.end();
//               } else {
//                 if (!retryResolved) {
//                   retryResolved = true;
//                   retryCleanup();
//                   reject(new Error("Retry client connection is not writable"));
//                 }
//               }
//             } catch (retryWriteError) {
//               if (!retryResolved) {
//                 retryResolved = true;
//                 retryCleanup();
//                 reject(
//                   new Error(
//                     `Failed to write to daemon on retry: ${
//                       retryWriteError instanceof Error
//                         ? retryWriteError.message
//                         : String(retryWriteError)
//                     }`
//                   )
//                 );
//               }
//             }
//           });

//           retryClient.on("data", (data) => {
//             retryResponse += data.toString();
//           });

//           retryClient.on("end", () => {
//             if (!retryResolved) {
//               retryResolved = true;
//               retryCleanup();
//               isResolved = true;
//               resolve(retryResponse);
//             }
//           });

//           retryClient.on("error", (retryError) => {
//             if (!retryResolved) {
//               retryResolved = true;
//               retryCleanup();
//               isResolved = true;
//               reject(
//                 new Error(
//                   `Daemon communication failed on retry: ${retryError.message}`
//                 )
//               );
//             }
//           });
//         } catch (startError) {
//           isResolved = true;
//           reject(new Error(`Failed to start daemon: ${startError}`));
//         }
//       } else {
//         isResolved = true;
//         reject(new Error(`Daemon communication error: ${error.message}`));
//       }
//     });
//   });
// }

// async function start(): Promise<void> {
//   if (daemonProcess || isStarting) {
//     return; // Already running or starting
//   }

//   isStarting = true;

//   return new Promise(async (resolve, reject) => {
//     try {
//       //const executablePath = await findCSharplyExecutable();
//       const executablePath =
//         "C:\\src\\CSharply\\artifacts\\bin\\CSharply\\debug\\CSharply.exe";
//       const daemonCommand = `${executablePath} daemon`;

//       daemonProcess = exec(daemonCommand, (error, stdout, stderr) => {
//         if (error) {
//           log(`Daemon error: ${error.message}`);
//           daemonProcess = undefined;
//         }
//         if (stderr) {
//           log(`Daemon stderr: ${stderr}`);
//         }
//         if (stdout) {
//           log(`Daemon stdout: ${stdout}`);
//         }
//       });

//       daemonProcess.on("spawn", async () => {
//         log("CSharply daemon started");

//         // Wait for the daemon to actually create the named pipe
//         const maxWait = 10000; // 10 seconds
//         const checkInterval = 500; // 500ms
//         let waited = 0;

//         while (waited < maxWait && !(await isPipeAvailable())) {
//           await new Promise((resolve) => setTimeout(resolve, checkInterval));
//           waited += checkInterval;
//         }

//         isStarting = false;

//         if (waited >= maxWait) {
//           log("Daemon started but pipe is not available");
//           reject(new Error("Daemon started but pipe is not available"));
//         } else {
//           log("Daemon pipe is ready");
//           resolve();
//         }
//       });

//       daemonProcess.on("error", (error) => {
//         log(`Failed to start daemon: ${error.message}`);
//         daemonProcess = undefined;
//         isStarting = false;
//         reject(error);
//       });

//       daemonProcess.on("exit", (code, signal) => {
//         log(`Daemon exited with code ${code}, signal ${signal}`);
//         daemonProcess = undefined;
//         isStarting = false;
//       });
//     } catch (error) {
//       isStarting = false;
//       reject(error);
//     }
//   });
// }

// export async function restart(): Promise<void> {
//   await stop();

//   // Wait a bit for the process to actually terminate
//   await wait(1000);

//   await start();
// }

// export async function stop(): Promise<void> {
//   if (!daemonProcess) {
//     return;
//   }

//   const cleanup = () => {
//     daemonProcess = undefined;
//     isStarting = false;
//   };

//   try {
//     log("Stopping daemon...");

//     // Set up exit handler and wait for process to exit
//     const exitPromise = new Promise<void>((resolve) => {
//       daemonProcess!.once("exit", () => {
//         log("Daemon exited gracefully");
//         cleanup();
//         resolve();
//       });
//     });

//     daemonProcess.kill("SIGTERM");
//     log("Sent SIGTERM to daemon");

//     // Create timeout promise without async inside Promise constructor
//     const timeoutPromise = (async () => {
//       await new Promise((resolve) => setTimeout(resolve, 5000));
//       if (daemonProcess && !daemonProcess.killed) {
//         log("Daemon didn't exit gracefully, force killing...");
//         daemonProcess.kill("SIGKILL");
//         await new Promise((resolve) => setTimeout(resolve, 1000));
//       }
//       cleanup();
//     })();

//     // Wait for either graceful exit or timeout
//     await Promise.race([exitPromise, timeoutPromise]);
//     log("Daemon stopped successfully");
//   } catch (error) {
//     log(`Error stopping daemon: ${error}`);
//     cleanup();
//   }
// }

// function isRunning(): boolean {
//   return (
//     daemonProcess !== undefined &&
//     !daemonProcess.killed &&
//     daemonProcess.exitCode === null &&
//     daemonProcess.signalCode === null
//   );
// }

// export async function testConnection(): Promise<boolean> {
//   try {
//     // First check if daemon process is running
//     if (!isRunning()) {
//       return false;
//     }

//     // Then check if pipe is available
//     if (!(await isPipeAvailable())) {
//       return false;
//     }

//     // Finally test actual communication
//     await organizeCode("// test");
//     return true;
//   } catch (error) {
//     log(`Connection test failed: ${error}`);
//     return false;
//   }
// }

// function getDaemonStatus(): {
//   isRunning: boolean;
//   pid?: number;
//   isStarting: boolean;
// } {
//   return {
//     isRunning: isRunning(),
//     pid: daemonProcess?.pid,
//     isStarting: isStarting,
//   };
// }

export type InverterReading = {
  id: number
  timestamp: string
  power: number
  voltage: number
  current: number
}

export type InverterInfo = {
  id: number
  serialNumber: string
  model: string
  manufacturer: string
}
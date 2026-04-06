export type InverterReading = {
  id: number
  timestamp: string
  power: number
  voltage: number
  current: number
  inverterInfoId: number
  addressId: number
}

export type InverterInfo = {
  id: number
  serialNumber: string
  model: string
  manufacturer: string
  addressId: number
}
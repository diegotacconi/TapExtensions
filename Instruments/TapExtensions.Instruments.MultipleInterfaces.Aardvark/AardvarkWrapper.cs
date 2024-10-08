/*=========================================================================
| Aardvark Interface Library
|--------------------------------------------------------------------------
| Copyright (c) 2003-2024 Total Phase, Inc.
| All rights reserved.
| www.totalphase.com
|
| Redistribution and use of this file in source and binary forms, with
| or without modification, are permitted provided that the following
| conditions are met:
|
| - Redistributions of source code must retain the above copyright
|   notice, this list of conditions, and the following disclaimer.
|
| - Redistributions in binary form must reproduce the above copyright
|   notice, this list of conditions, and the following disclaimer in the
|   documentation or other materials provided with the distribution.
|
| - This file must only be used to interface with Total Phase products.
|   The names of Total Phase and its contributors must not be used to
|   endorse or promote products derived from this software.
|
| THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
| "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING BUT NOT
| LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS
| FOR A PARTICULAR PURPOSE, ARE DISCLAIMED.  IN NO EVENT WILL THE
| COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT,
| INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING
| BUT NOT LIMITED TO PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
| LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
| CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
| LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN
| ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
| POSSIBILITY OF SUCH DAMAGE.
|--------------------------------------------------------------------------
| To access Total Phase Aardvark devices through the API:
|
| 1) Use one of the following shared objects:
|      aardvark.so       --  Linux or macOS shared object
|      aardvark.dll      --  Windows dynamic link library
|
| 2) Along with one of the following language modules:
|      aardvark.c/h      --  C/C++ API header file and interface module
|      aardvark_py.py    --  Python API
|      aardvark.cs       --  C# .NET source
|      aardvark_net.dll  --  Compiled .NET binding
|      aardvark.bas      --  Visual Basic 6 API
 ========================================================================*/

using System;
using System.Runtime.InteropServices;
using OpenTap;

// ReSharper disable InconsistentNaming

namespace TapExtensions.Instruments.MultipleInterfaces.Aardvark
{
    #region ENUMS

    public enum AardvarkStatus
    {
        /* General codes (0 to -99) */
        AA_OK = 0,
        AA_UNABLE_TO_LOAD_LIBRARY = -1,
        AA_UNABLE_TO_LOAD_DRIVER = -2,
        AA_UNABLE_TO_LOAD_FUNCTION = -3,
        AA_INCOMPATIBLE_LIBRARY = -4,
        AA_INCOMPATIBLE_DEVICE = -5,
        AA_COMMUNICATION_ERROR = -6,
        AA_UNABLE_TO_OPEN = -7,
        AA_UNABLE_TO_CLOSE = -8,
        AA_INVALID_HANDLE = -9,
        AA_CONFIG_ERROR = -10,

        /* I2C codes (-100 to -199) */
        AA_I2C_NOT_AVAILABLE = -100,
        AA_I2C_NOT_ENABLED = -101,
        AA_I2C_READ_ERROR = -102,
        AA_I2C_WRITE_ERROR = -103,
        AA_I2C_SLAVE_BAD_CONFIG = -104,
        AA_I2C_SLAVE_READ_ERROR = -105,
        AA_I2C_SLAVE_TIMEOUT = -106,
        AA_I2C_DROPPED_EXCESS_BYTES = -107,
        AA_I2C_BUS_ALREADY_FREE = -108,

        /* SPI codes (-200 to -299) */
        AA_SPI_NOT_AVAILABLE = -200,
        AA_SPI_NOT_ENABLED = -201,
        AA_SPI_WRITE_ERROR = -202,
        AA_SPI_SLAVE_READ_ERROR = -203,
        AA_SPI_SLAVE_TIMEOUT = -204,
        AA_SPI_DROPPED_EXCESS_BYTES = -205,

        /* GPIO codes (-400 to -499) */
        AA_GPIO_NOT_AVAILABLE = -400
    }

    public enum EConfigMode
    {
        [Display("GPIO Only")] GPIO_ONLY = 0x00,
        [Display("SPI + GPIO")] SPI_GPIO = 0x01,
        [Display("I2C + GPIO")] GPIO_I2C = 0x02,
        [Display("I2C + SPI")] SPI_I2C = 0x03
    }

    public enum ETargetPower
    {
        Off = 0x00,
        [Display("5 Volts")] On5V0 = 0x03
    }

    public enum EI2cPullup
    {
        Off = 0x00,
        On = 0x03
    }

    public enum AardvarkI2cFlags
    {
        AA_I2C_NO_FLAGS = 0x00,
        AA_I2C_10_BIT_ADDR = 0x01,
        AA_I2C_COMBINED_FMT = 0x02,
        AA_I2C_NO_STOP = 0x04,
        AA_I2C_SIZED_READ = 0x10,
        AA_I2C_SIZED_READ_EXTRA1 = 0x20
    }

    public enum AardvarkI2cStatus
    {
        AA_I2C_STATUS_OK = 0,
        AA_I2C_STATUS_BUS_ERROR = 1,
        AA_I2C_STATUS_SLA_ACK = 2,
        AA_I2C_STATUS_SLA_NACK = 3,
        AA_I2C_STATUS_DATA_NACK = 4,
        AA_I2C_STATUS_ARB_LOST = 5,
        AA_I2C_STATUS_BUS_LOCKED = 6,
        AA_I2C_STATUS_LAST_DATA_ACK = 7
    }

    public enum AardvarkSpiPolarity
    {
        AA_SPI_POL_RISING_FALLING = 0,
        AA_SPI_POL_FALLING_RISING = 1
    }

    public enum AardvarkSpiPhase
    {
        AA_SPI_PHASE_SAMPLE_SETUP = 0,
        AA_SPI_PHASE_SETUP_SAMPLE = 1
    }

    public enum AardvarkSpiBitorder
    {
        AA_SPI_BITORDER_MSB = 0,
        AA_SPI_BITORDER_LSB = 1
    }

    public enum AardvarkSpiSSPolarity
    {
        AA_SPI_SS_ACTIVE_LOW = 0,
        AA_SPI_SS_ACTIVE_HIGH = 1
    }

    public enum AardvarkGpioBits
    {
        AA_GPIO_SCL = 0x01,
        AA_GPIO_SDA = 0x02,
        AA_GPIO_MISO = 0x04,
        AA_GPIO_SCK = 0x08,
        AA_GPIO_MOSI = 0x10,
        AA_GPIO_SS = 0x20
    }

    #endregion

    internal class AardvarkWrapper
    {
        #region VERSION

        /*
         * Aardvark version matrix.
         *
         * This matrix describes the various version dependencies
         * of Aardvark components.  It can be used to determine
         * which component caused an incompatibility error.
         *
         * All version numbers are of the format:
         *   (major << 8) | minor
         *
         * ex. v1.20 would be encoded as:  0x0114
         */
        [StructLayout(LayoutKind.Sequential)]
        public struct AardvarkVersion
        {
            /* Software, firmware, and hardware versions. */
            public ushort software;
            public ushort firmware;
            public ushort hardware;

            /* Firmware requires that software must be >= this version. */
            public ushort sw_req_by_fw;

            /* Software requires that firmware must be >= this version. */
            public ushort fw_req_by_sw;

            /* Software requires that the API interface must be >= this version. */
            public ushort api_req_by_sw;
        }

        #endregion

        #region GENERAL API

        [StructLayout(LayoutKind.Sequential)]
        public struct AardvarkExt
        {
            /* Version matrix */
            public AardvarkVersion version;

            /* Features of this device. */
            public int features;
        }

        /*
         * Get a list of ports to which Aardvark devices are attached.
         *
         * nelem   = maximum number of elements to return
         * devices = array into which the port numbers are returned
         *
         * Each element of the array is written with the port number.
         * Devices that are in-use are ORed with AA_PORT_NOT_FREE (0x8000).
         *
         * ex.  devices are attached to ports 0, 1, 2
         *      ports 0 and 2 are available, and port 1 is in-use.
         *      array => 0x0000, 0x8001, 0x0002
         *
         * If the array is NULL, it is not filled with any values.
         * If there are more devices than the array size, only the
         * first nmemb port numbers will be written into the array.
         *
         * Returns the number of devices found, regardless of the
         * array size.
         *
         * AA_PORT_NOT_FREE = 0x8000
         */
        [DllImport("aardvark")]
        public static extern int net_aa_find_devices(int num_devices, [Out] ushort[] devices);

        /*
         * Get a list of ports to which Aardvark devices are attached.
         *
         * This function is the same as aa_find_devices() except that
         * it returns the unique IDs of each Aardvark device. The IDs
         * are guaranteed to be non-zero if valid.
         *
         * The IDs are the unsigned integer representation of the 10-digit
         * serial numbers.
         */
        [DllImport("aardvark")]
        public static extern int net_aa_find_devices_ext(int num_devices, [Out] ushort[] devices, int num_ids,
            [Out] uint[] unique_ids);

        /*
         * Open the Aardvark port.
         *
         * The port number is a zero-indexed integer.
         *
         * The port number is the same as that obtained from the
         * aa_find_devices() function above.
         *
         * Returns an Aardvark handle, which is guaranteed to be
         * greater than zero if it is valid.
         *
         * This function is recommended for use in simple applications
         * where extended information is not required. For more complex
         * applications, the use of aa_open_ext() is recommended.
         */
        [DllImport("aardvark")]
        public static extern int net_aa_open(int port_number);

        /*
         * Open the Aardvark port, returning extended information
         * in the supplied structure. Behavior is otherwise identical
         * to aa_open() above. If 0 is passed as the pointer to the
         * structure, this function is exactly equivalent to aa_open().
         *
         * The structure is zeroed before the open is attempted.
         * It is filled with whatever information is available.
         *
         * For example, if the firmware version is not filled, then
         * the device could not be queried for its version number.
         *
         * This function is recommended for use in complex applications
         * where extended information is required. For more simple
         * applications, the use of aa_open() is recommended.
         */
        [DllImport("aardvark")]
        public static extern int net_aa_open_ext(int port_number, ref AardvarkExt aa_ext);

        /* Close the Aardvark port. */
        [DllImport("aardvark")]
        public static extern int net_aa_close(int aardvark);

        /*
         * Return the port for this Aardvark handle.
         * The port number is a zero-indexed integer.
         */
        [DllImport("aardvark")]
        public static extern int net_aa_port(int aardvark);

        /*
         * Return the device features as a bit-mask of values, or
         * an error code if the handle is not valid.
         *
         * AA_FEATURE_SPI = 0x01
         * AA_FEATURE_I2C = 0x02
         * AA_FEATURE_GPIO = 0x08
         */
        [DllImport("aardvark")]
        public static extern int net_aa_features(int aardvark);

        /*
         * Return the unique ID for this Aardvark adapter.
         * IDs are guaranteed to be non-zero if valid.
         * The ID is the unsigned integer representation of the
         * 10-digit serial number.
         */
        [DllImport("aardvark")]
        public static extern uint net_aa_unique_id(int aardvark);

        /*
         * Return the status string for the given status code.
         * If the code is not valid or the library function cannot
         * be loaded, return a NULL string.
         */
        [DllImport("aardvark")]
        public static extern IntPtr net_aa_status_string(int status);

        /*
         * Enable logging to a file.  The handle must be standard file
         * descriptor.  In C, a file descriptor can be obtained by using
         * the ANSI C function "open" or by using the function "fileno"
         * on a FILE* stream.  A FILE* stream can be obtained using "fopen"
         * or can correspond to the common "stdout" or "stderr" --
         * available when including stdlib.h
         *
         * AA_LOG_STDOUT = 1
         * AA_LOG_STDERR = 2
         */
        [DllImport("aardvark")]
        public static extern int net_aa_log(int aardvark, int level, int handle);

        /*
         * Return the version matrix for the device attached to the
         * given handle. If the handle is 0 or invalid, only the
         * software and required api versions are set.
         */
        [DllImport("aardvark")]
        public static extern int net_aa_version(int aardvark, ref AardvarkVersion version);

        /*
         * Configure the device by enabling/disabling I2C, SPI, and GPIO functions.
         *
         * AA_CONFIG_GPIO_ONLY = 0x00
         * AA_CONFIG_SPI_GPIO = 0x01
         * AA_CONFIG_GPIO_I2C = 0x02
         * AA_CONFIG_SPI_I2C = 0x03
         * AA_CONFIG_QUERY = 0x80
         */
        [DllImport("aardvark")]
        public static extern int net_aa_configure(int aardvark, EConfigMode config);

        /*
         * Configure the Aardvark adapter's target power supply pins (5 Volts).
         *
         * AA_TARGET_POWER_NONE = 0x00
         * AA_TARGET_POWER_BOTH = 0x03
         * AA_TARGET_POWER_QUERY = 0x80
         */
        [DllImport("aardvark")]
        public static extern int net_aa_target_power(int aardvark, byte power_mask);

        /*
         * Sleep for the specified number of milliseconds
         * Accuracy depends on the operating system scheduler
         * Returns the number of milliseconds slept
         */
        [DllImport("aardvark")]
        public static extern uint net_aa_sleep_ms(uint milliseconds);

        #endregion

        #region ASYNC MESSAGE POLLING

        /*
         * Polling function to check if there are any asynchronous
         * messages pending for processing. The function takes a timeout
         * value in units of milliseconds. If the timeout is < 0, the
         * function will block until data is received. If the timeout is 0,
         * the function will perform a non-blocking check.
         *
         * AA_ASYNC_NO_DATA = 0x00
         * AA_ASYNC_I2C_READ = 0x01
         * AA_ASYNC_I2C_WRITE = 0x02
         * AA_ASYNC_SPI = 0x04
         */
        [DllImport("aardvark")]
        public static extern int net_aa_async_poll(int aardvark, int timeout);

        #endregion

        #region I2C API

        /* Free the I2C bus. */
        [DllImport("aardvark")]
        public static extern int net_aa_i2c_free_bus(int aardvark);

        /*
         * Set the I2C bit rate in kilohertz. If a zero is passed as the
         * bitrate, the bitrate is unchanged and the current bitrate is
         * returned.
         */
        [DllImport("aardvark")]
        public static extern int net_aa_i2c_bitrate(int aardvark, int bitrate_khz);

        /*
         * Set the bus lock timeout. If a zero is passed as the timeout,
         * the timeout is unchanged and the current timeout is returned.
         */
        [DllImport("aardvark")]
        public static extern int net_aa_i2c_bus_timeout(int aardvark, ushort timeout_ms);

        /* Read a stream of bytes from the I2C slave device. */
        [DllImport("aardvark")]
        public static extern int net_aa_i2c_read(int aardvark, ushort slave_addr, AardvarkI2cFlags flags,
            ushort num_bytes, [Out] byte[] data_in);

        /*
         * Read a stream of bytes from the I2C slave device.
         * This API function returns the number of bytes read into
         * the num_read variable. The return value of the function
         * is a status code.
         */
        [DllImport("aardvark")]
        public static extern int net_aa_i2c_read_ext(int aardvark, ushort slave_addr, AardvarkI2cFlags flags,
            ushort num_bytes, [Out] byte[] data_in, ref ushort num_read);

        /* Write a stream of bytes to the I2C slave device. */
        [DllImport("aardvark")]
        public static extern int net_aa_i2c_write(int aardvark, ushort slave_addr, AardvarkI2cFlags flags,
            ushort num_bytes, [In] byte[] data_out);

        /*
         * Write a stream of bytes to the I2C slave device.
         * This API function returns the number of bytes written into
         * the num_written variable. The return value of the function
         * is a status code.
         */
        [DllImport("aardvark")]
        public static extern int net_aa_i2c_write_ext(int aardvark, ushort slave_addr, AardvarkI2cFlags flags,
            ushort num_bytes, [In] byte[] data_out, ref ushort num_written);

        /*
         * Do an atomic write+read to an I2C slave device by first
         * writing a stream of bytes to the I2C slave device and then
         * reading a stream of bytes back from the same slave device.
         * This API function returns the number of bytes written into
         * the num_written variable and the number of bytes read into
         * the num_read variable. The return value of the function is
         * the status given as (read_status << 8) | (write_status).
         */
        [DllImport("aardvark")]
        public static extern int net_aa_i2c_write_read(int aardvark, ushort slave_addr, AardvarkI2cFlags flags,
            ushort out_num_bytes, [In] byte[] out_data, ref ushort num_written, ushort in_num_bytes,
            [Out] byte[] in_data, ref ushort num_read);

        /* Enable/Disable the Aardvark as an I2C slave device */
        [DllImport("aardvark")]
        public static extern int
            net_aa_i2c_slave_enable(int aardvark, byte addr, ushort maxTxBytes, ushort maxRxBytes);

        [DllImport("aardvark")]
        public static extern int net_aa_i2c_slave_disable(int aardvark);

        /*
         * Set the slave response in the event the Aardvark is put
         * into slave mode and contacted by a Master.
         */
        [DllImport("aardvark")]
        public static extern int net_aa_i2c_slave_set_response(int aardvark, byte num_bytes, [In] byte[] data_out);

        /*
         * Return number of bytes written from a previous
         * Aardvark->I2C_master transmission. Since the transmission is
         * happening asynchronously with respect to the PC host
         * software, there could be responses queued up from many
         * previous write transactions.
         */
        [DllImport("aardvark")]
        public static extern int net_aa_i2c_slave_write_stats(int aardvark);

        /* Read the bytes from an I2C slave reception */
        [DllImport("aardvark")]
        public static extern int net_aa_i2c_slave_read(int aardvark, ref byte addr, ushort num_bytes,
            [Out] byte[] data_in);

        /* Extended functions that return status code */
        [DllImport("aardvark")]
        public static extern int net_aa_i2c_slave_write_stats_ext(int aardvark, ref ushort num_written);

        [DllImport("aardvark")]
        public static extern int net_aa_i2c_slave_read_ext(int aardvark, ref byte addr, ushort num_bytes,
            [Out] byte[] data_in, ref ushort num_read);

        /*
         * Configure the I2C pullup resistors (2.2k resistors).
         *
         * AA_I2C_PULLUP_NONE = 0x00
         * AA_I2C_PULLUP_BOTH = 0x03
         * AA_I2C_PULLUP_QUERY = 0x80
         */
        [DllImport("aardvark")]
        public static extern int net_aa_i2c_pullup(int aardvark, byte pullup_mask);

        #endregion

        #region SPI API

        /*
         * Set the SPI bit rate in kilohertz. If a zero is passed as the
         * bitrate, the bitrate is unchanged and the current bitrate is
         * returned.
         */
        [DllImport("aardvark")]
        public static extern int net_aa_spi_bitrate(int aardvark, int bitrate_khz);

        /*
         * These configuration parameters specify how to clock the
         * bits that are sent and received on the Aardvark SPI
         * interface.
         *
         *   The polarity option specifies which transition
         *   constitutes the leading edge and which transition is the
         *   falling edge.  For example, AA_SPI_POL_RISING_FALLING
         *   would configure the SPI to idle the SCK clock line low.
         *   The clock would then transition low-to-high on the
         *   leading edge and high-to-low on the trailing edge.
         *
         *   The phase option determines whether to sample or setup on
         *   the leading edge.  For example, AA_SPI_PHASE_SAMPLE_SETUP
         *   would configure the SPI to sample on the leading edge and
         *   setup on the trailing edge.
         *
         *   The bitorder option is used to indicate whether LSB or
         *   MSB is shifted first.
         *
         * See the diagrams in the Aardvark datasheet for
         * more details.
         */

        /* Configure the SPI master or slave interface */
        [DllImport("aardvark")]
        public static extern int net_aa_spi_configure(int aardvark, AardvarkSpiPolarity polarity,
            AardvarkSpiPhase phase, AardvarkSpiBitorder bitorder);

        /* Write a stream of bytes to the downstream SPI slave device. */
        [DllImport("aardvark")]
        public static extern int net_aa_spi_write(int aardvark, ushort out_num_bytes, [In] byte[] data_out,
            ushort in_num_bytes, [Out] byte[] data_in);

        /* Enable the Aardvark as an SPI slave device */
        [DllImport("aardvark")]
        public static extern int net_aa_spi_slave_enable(int aardvark);

        /* Disable the Aardvark as an SPI slave device */
        [DllImport("aardvark")]
        public static extern int net_aa_spi_slave_disable(int aardvark);

        /*
         * Set the slave response in the event the Aardvark is put
         * into slave mode and contacted by a Master.
         */
        [DllImport("aardvark")]
        public static extern int net_aa_spi_slave_set_response(int aardvark, byte num_bytes, [In] byte[] data_out);

        /* Read the bytes from an SPI slave reception */
        [DllImport("aardvark")]
        public static extern int net_aa_spi_slave_read(int aardvark, ushort num_bytes, [Out] byte[] data_in);

        /*
         * Change the output polarity on the SS line.
         *
         * Note: When configured as an SPI slave, the Aardvark will
         * always be setup with SS as active low. Hence this function
         * only affects the SPI master functions on the Aardvark.
         */
        [DllImport("aardvark")]
        public static extern int net_aa_spi_master_ss_polarity(int aardvark, AardvarkSpiSSPolarity polarity);

        #endregion

        #region GPIO API

        /*
         * The AardvarkGpioBits type maps the named lines on the
         * Aardvark I2C/SPI line to bit positions in the GPIO API.
         * All GPIO API functions will index these lines through an
         * 8-bit masked value.  Thus, each bit position in the mask
         * can be referred back its corresponding line through the
         * enumerated type.
         */

        /*
         * Configure the GPIO, specifying the direction of each bit.
         *
         * A call to this function will not change the value of the pullup
         * mask in the Aardvark. This is illustrated by the following
         * example:
         *   (1) Direction mask is first set to 0x00
         *   (2) Pullup is set to 0x01
         *   (3) Direction mask is set to 0x01
         *   (4) Direction mask is later set back to 0x00.
         *
         * The pullup will be active after (4).
         *
         * On Aardvark power-up, the default value of the direction
         * mask is 0x00.
         *
         * AA_GPIO_DIR_INPUT = 0
         * AA_GPIO_DIR_OUTPUT = 1
         */
        [DllImport("aardvark")]
        public static extern int net_aa_gpio_direction(int aardvark, byte direction_mask);

        /*
         * Enable an internal pullup on any of the GPIO input lines.
         *
         * Note: If a line is configured as an output, the pullup bit
         * for that line will be ignored, though that pullup bit will
         * be cached in case the line is later configured as an input.
         *
         * By default the pullup mask is 0x00.
         *
         * AA_GPIO_PULLUP_OFF = 0
         * AA_GPIO_PULLUP_ON = 1
         */
        [DllImport("aardvark")]
        public static extern int net_aa_gpio_pullup(int aardvark, byte pullup_mask);

        /*
         * Read the current digital values on the GPIO input lines.
         *
         * The bits will be ordered as described by AA_GPIO_BITS.  If a
         * line is configured as an output, its corresponding bit
         * position in the mask will be undefined.
         */
        [DllImport("aardvark")]
        public static extern int net_aa_gpio_get(int aardvark);

        /*
         * Set the outputs on the GPIO lines.
         *
         * Note: If a line is configured as an input, it will not be
         * affected by this call, but the output value for that line
         * will be cached in the event that the line is later
         * configured as an output.
         */
        [DllImport("aardvark")]
        public static extern int net_aa_gpio_set(int aardvark, byte value);

        /*
         * Block until there is a change on the GPIO input lines.
         * Pins configured as outputs will be ignored.
         *
         * The function will return either when a change has occurred or
         * the timeout expires. The timeout, specified in milliseconds, has
         * a precision of ~16 ms. The maximum allowable timeout is
         * approximately 4 seconds. If the timeout expires, this function
         * will return the current state of the GPIO lines.
         *
         * This function will return immediately with the current value
         * of the GPIO lines for the first invocation after any of the
         * following functions are called: aa_configure,
         * aa_gpio_direction, or aa_gpio_pullup.
         *
         * If the function aa_gpio_get is called before calling
         * aa_gpio_change, aa_gpio_change will only register any changes
         * from the value last returned by aa_gpio_get.
         */
        [DllImport("aardvark")]
        public static extern int net_aa_gpio_change(int aardvark, ushort timeout);

        #endregion
    }
}
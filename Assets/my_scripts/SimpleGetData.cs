using System;

using System.Numerics;

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using brainflow;
using brainflow.math;

public class SimpleGetData : MonoBehaviour
{
    private BoardShim board_shim = null;
    private int sampling_rate = 0;
    private int nfft;
    private int[] eeg_channels;
    
    // Start is called before the first frame update
    void Start()
    {
        try
        {
            BoardShim.set_log_file("brainflow_log.txt");
            
            BoardShim.enable_dev_board_logger();

            BrainFlowInputParams input_params = new BrainFlowInputParams();
            int board_id = (int)BoardIds.SYNTHETIC_BOARD;

            BoardDescr board_descr = BoardShim.get_board_descr<BoardDescr>(board_id);
            sampling_rate = board_descr.sampling_rate;
            


            board_shim = new BoardShim(board_id, input_params);
            board_shim.prepare_session();
            board_shim.start_stream(450000, "file://brainflow_data.csv:w");
            //sampling_rate = BoardShim.get_sampling_rate(board_id);
            //eeg_channels = BoardShim.get_eeg_channels(board_id);

            eeg_channels = board_descr.eeg_channels;
            
            // use second channel of synthetic board to see 'alpha'
            

            Debug.Log("Brainflow streaming was started");
            
        }
        catch (BrainFlowException e)
        {
            Debug.Log(e);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (board_shim == null)
        {
            return;
        }
        int number_of_data_points = sampling_rate * 4;
        double[,] data = board_shim.get_current_board_data(number_of_data_points);
        // check https://brainflow.readthedocs.io/en/stable/index.html for api ref and more code samples
        //Debug.Log(data.GetRow (eeg_channels[2]));
        Debug.Log("Num elements: " + data.GetLength(1));

        nfft = DataFilter.get_nearest_power_of_two(sampling_rate);
        //Debug.Log("Nfft:"+nfft);
        

        //Band Power
        int channel = eeg_channels[1];

        double[] detrend = DataFilter.detrend(data.GetRow(channel), (int)DetrendOperations.LINEAR);
        print("Detrend: ");
        Debug.Log(string.Join (", ", detrend));
        Debug.Log(detrend.GetType());
        Debug.Log(nfft.GetType());
        int overlap = nfft/2;
        Debug.Log(overlap.GetType());
        Debug.Log(sampling_rate.GetType());
        Debug.Log(((int)WindowFunctions.HANNING).GetType());
        
        Tuple<double[], double[]> psd = DataFilter.get_psd_welch (detrend, nfft, overlap, sampling_rate, (int)WindowFunctions.HANNING);
            //Tuple<double[], double[]> psd = DataFilter.get_psd_welch (data.GetRow (eeg_channels[i]), nfft, nfft / 2, sampling_rate, (int)WindowFunctions.HANNING);
        double band_power_alpha = DataFilter.get_band_power (psd, 7.0, 13.0);
        double band_power_beta = DataFilter.get_band_power (psd, 14.0, 30.0);
        Console.WriteLine ("Alpha/Beta Ratio:" + (band_power_alpha/ band_power_beta));
        
        
        for (int i = 0; i < eeg_channels.Length; i++){
            /*
            Tuple<double[], int[]> wavelet_data = DataFilter.perform_wavelet_transform(data.GetRow (eeg_channels[i]), "db4", 3);
            for (int j = 0; j < wavelet_data.Item2[0]; j++){
                Console.Write (wavelet_data.Item1[j] + " ");
            }
            */

            //Fast Fourier Transform
            /*
            Complex[] fft_data = DataFilter.perform_fft (data.GetRow (eeg_channels[i]), 0, 64, (int)WindowFunctions.HAMMING);
            print(fft_data.Length);
            Console.WriteLine ("Fft data:");
            Console.WriteLine ("FFT [{0}]", string.Join (", ", fft_data));
            Debug.Log(string.Join (", ", fft_data));
            */
            
            
            

            /*
            Tuple<double[], double[]> bands = DataFilter.get_avg_band_powers (data, eeg_channels, sampling_rate, true);
            double[] feature_vector = bands.Item1.Concatenate (bands.Item2);
            BrainFlowModelParams model_params = new BrainFlowModelParams ((int)BrainFlowMetrics.CONCENTRATION, (int)BrainFlowClassifiers.REGRESSION);
            MLModel concentration = new MLModel (model_params);
            concentration.prepare ();
            Console.WriteLine ("Concentration: " + concentration.predict (feature_vector));
            concentration.release ();
            
            */
        }
        

    }

    // you need to call release_session and ensure that all resources correctly released
    private void OnDestroy()
    {
        if (board_shim != null)
        {
            try
            {
                board_shim.release_session();
            }
            catch (BrainFlowException e)
            {
                Debug.Log(e);
            }
            Debug.Log("Brainflow streaming was stopped");
        }
    }
}



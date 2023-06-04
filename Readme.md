# Water Level Prediction

This repository hold the code for my own home brewn water level prediction system. The system is based on an ESP32 camera module that can take pictures of my water tank, a .NET Core Service Worker orchestrating getting pictures of the water tank from the ESP32 and calling the Microsoft CustomVision api to predict the water level as well as a Phillips Hue 220 volt plug controlling the water pump in the water tank, delivering water to the relevant places.

## Introduction

The idea came together after having installed a 1000 liter water tank in the very back of the garden collecting water from 25% of the roof area. The intend is to use this water for irrigation of the vegetable garden in the other end combined with the wish to automate this.

For a start a water pump was installed in the water tank and a hose installed with outputs by the vegetable garden and a minor water tank close to by. The water pump is connected to and controlled by a Phillips Hue plug. In this way it was possible to control the water pump from the mobile Phillips Hue app.

As this basically requires a look at the water tank to get an idea about the water level, this was still rather cumbersome to control the moving of the water.

## Image recognition to deduct water levels

Later on the idea came to use Machine Learning and images classification to deduct the water level from pictures of the water tank. For this the Microsoft Custom Vision service was chosen.

The <a href="https://www.customvision.ai/" target="_blank">Microsoft Custom Vision</a> system consists of both an API and a website to control and train the model. It allows for uploading pictures of the images and manually classifying them as well as initiating training.

The classification system was a set of percentage categories from 0% to 100% with 5% increments.

So basically a lot of water tank pictures with different water levels and lightings need to be uploaded and classified by adding the the percentage categories. Once this is done the model can be trained and the used.

## ESC32 setup

To be described.

## .NET Core Service Worker

The Water Level Prediction Worker Service is a .NET Core Service Worker and is bootstraped in Program.cs.

The Worker Service loops over the following tasks:

* Fetch the image from the server
  * Call Capture on the server
  * Download the bytes of the newly captured image
* Post the image for prediction and return the prediction results
* Extract the water level tag for the best prediction result
* Store the image in a folder with the tag name
* Store the prediction result in the same folder
* Check the Sunset Service for sunset and twilight data
* Extend the wait time if it is night

## Architecture

To be described.

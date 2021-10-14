// MathLibrary.cpp : Defines the exported functions for the DLL.
#include "pch.h" // use stdafx.h in Visual Studio 2017 and earlier
#include <utility>
#include <limits.h>
#include "MathLibrary.h"

#include "SVSCamDLL.h"



// DLL internal state variables:
static unsigned long long previous_;  // Previous value, if any
static unsigned long long current_;   // Current sequence value
static unsigned index_;               // Current seq. position

// -----------------------------------------------------------------------------
// Joe Added:
// SVSCamDLL.h/cpp test
static SVSCamDLL m_SVSCamDLLInst;
static long m_nPhotoCounter = 0;


// Initialize a Fibonacci relation sequence
// such that F(0) = a, F(1) = b.
// This function must be called before any other function.
void fibonacci_init(
    const unsigned long long a,
    const unsigned long long b)
{
    index_ = 0;
    current_ = a;
    previous_ = b; // see special case when initialized
}

// Produce the next value in the sequence.
// Returns true on success, false on overflow.
bool fibonacci_next()
{
    // check to see if we'd overflow result or position
    if ((ULLONG_MAX - previous_ < current_) ||
        (UINT_MAX == index_))
    {
        return false;
    }

    // Special case when index == 0, just return b value
    if (index_ > 0)
    {
        // otherwise, calculate next sequence value
        previous_ += current_;
    }
    std::swap(current_, previous_);
    ++index_;
    return true;
}

// Get the current value in the sequence.
unsigned long long fibonacci_current()
{
    return current_;
}

// Get the current index position in the sequence.
unsigned fibonacci_index()
{
    return index_;
}


// ----------------------------- BEGIN: JT API for SVS Camera: Software Trigger Capture --------------------------------- //

int jtapi_svs_cam_open()
{
    m_nPhotoCounter = 0;
    int ret = m_SVSCamDLLInst.Open(); 
    return ret;
}



bool jtapi_svs_cam_sw_trigger_and_save()
{
    m_nPhotoCounter++;
    bool bRet = m_SVSCamDLLInst.SoftTriggerAndSavePhoto(); 
    return bRet;
}


bool jtapi_svs_cam_close()
{
    m_SVSCamDLLInst.Close();
    m_nPhotoCounter = 0; 
    return true;
}







// ----------------------------- END: JT API for SVS Camera: Software Trigger Capture --------------------------------- //






